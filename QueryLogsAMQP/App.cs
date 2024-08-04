using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using DnsServerCore.ApplicationCommon;
using Newtonsoft.Json;
using TechnitiumLibrary.Net.Dns;
using TechnitiumLibrary.Net.Dns.ResourceRecords;

namespace QueryLogsAMQP;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable once UnusedType.Global
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
public sealed class App : IDnsApplication, IDnsQueryLogger {
    /* Instance variables */
    private IDnsServer _dnsServer;
    private AppConfig _appConfig;

    private readonly Timer _queueProcessingTimer;

    private readonly ConcurrentQueue<RawQueryLogEntry> _queuedQueryLogEntries = new ConcurrentQueue<RawQueryLogEntry>();

    public App() {
        Console.WriteLine("QueryLogsAMQP: Instantiated App");
        _queueProcessingTimer = new Timer(async delegate(object state) {
            //Console.WriteLine("QueryLogsAMQP: Pushing logs #");
            //_dnsServer?.WriteLog("Pushing logs");

            try {
                await PushLogBatch();
            } catch(Exception e) {
                if(_dnsServer != null) {
                    _dnsServer.WriteLog("Failed to push log batch !");
                    _dnsServer.WriteLog(e);
                } else {
                    Console.Error.WriteLine("Failed to push log batch !");
                    Console.Error.WriteLine(e);
                }
            } finally {
                try {
                    _queueProcessingTimer!.Change(5000, Timeout.Infinite);
                } catch(ObjectDisposedException) { }
            }
        });
    }

    private async Task PushLogBatch() {
        Queue<RawQueryLogEntry> logEntries = new Queue<RawQueryLogEntry>();

        while(true) {
            // Fetching available log entries
            while(logEntries.Count < _appConfig.SenderBatchMaxSize &&
                  _queuedQueryLogEntries.TryDequeue(out RawQueryLogEntry log)) {
                logEntries.Enqueue(log);
            }

            if(logEntries.Count < 1) {
                break;
            }

            // Attempting to push batch, and re-enqueuing remaining on
            //  failure without incrementing failure count.

            IConnection ampqConnection = null;
            try {
                //Uri amqpUri = _appConfig.GetConnectionString();
                //Console.WriteLine(amqpUri.ToString());
                ampqConnection = new ConnectionFactory {
                    HostName = _appConfig.AmqpHost,
                    Port = _appConfig.AmqpPort,
                    UserName = _appConfig.AmqpAuthUsername,
                    Password = _appConfig.AmqpAuthPassword,
                    VirtualHost = _appConfig.AmqpVirtualHost,
                    //Uri = amqpUri
                }.CreateConnection();

                //Scheme = "amqp",

                IModel amqpChannel = ampqConnection.CreateModel();

                IBasicProperties props = amqpChannel.CreateBasicProperties();
                props.ContentType = "application/json";
                props.DeliveryMode = 2;
                //props.Headers = new Dictionary<string, object>();
                //props.Headers.Add("latitude",  51.5252949);
                //props.Headers.Add("longitude", -0.0905493);

                while(logEntries.TryDequeue(out RawQueryLogEntry logEntry)) {
                    // Attempting to push the message, or re-enqueuing it on failure.
                    try {
                        ProcessedQueryLogEntry messageData = new ProcessedQueryLogEntry();

                        messageData.timestamp = new DateTimeOffset(logEntry.Timestamp.ToUniversalTime())
                            .ToUnixTimeMilliseconds().ToString();

                        messageData.clientIp = logEntry.RemoteEp.Address.ToString();

                        messageData.protocol = logEntry.Protocol.ToString();

                        messageData.responseType = logEntry.Response.Tag == null
                            ? (int)DnsServerResponseType.Recursive
                            : (int)(DnsServerResponseType)logEntry.Response.Tag;

                        messageData.rCode = (int)logEntry.Response.RCODE;

                        if(logEntry.Request.Question.Count > 0) {
                            DnsQuestionRecord query = logEntry.Request.Question[0];
                            messageData.qName = query.Name.ToLower();
                            messageData.qType = (int)query.Type;
                            messageData.qClass = (int)query.Class;
                        } else {
                            messageData.qName = null;
                            messageData.qType = -1;
                            messageData.qClass = -1;
                        }

                        // ReSharper disable once ConvertIfStatementToSwitchStatement
                        if(logEntry.Response.Answer.Count == 0) {
                            messageData.answer = null;
                        } else if(logEntry.Response.Answer.Count > 2 && logEntry.Response.IsZoneTransfer) {
                            messageData.answer = "[ZONE TRANSFER]";
                        } else {
                            string answer = null;
                            foreach(DnsResourceRecord dnsResourceRecord in logEntry.Response.Answer) {
                                if(answer is null) {
                                    answer = dnsResourceRecord.RDATA.ToString();
                                } else {
                                    answer += ", " + dnsResourceRecord.RDATA;
                                }
                            }

                            messageData.answer = answer;
                        }

                        //string jsonString = JsonConvert.SerializeObject(messageData);
                        //Console.WriteLine(jsonString);
                        byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(messageData));

                        amqpChannel.BasicPublish(_appConfig.AmqpExchangeName, _appConfig.AmqpRoutingKey,
                            props, messageBodyBytes);
                    } catch(Exception e) {
                        _dnsServer.WriteLog(e);
                        logEntry.PushAttemptCount++;
                        _queuedQueryLogEntries.Enqueue(logEntry);
                    }
                }
            } catch(Exception e) {
                _dnsServer.WriteLog("AMQP connection appears to have failed !");
                _dnsServer.WriteLog(e);
            } finally {
                while(logEntries.TryDequeue(out RawQueryLogEntry logEntry)) {
                    _queuedQueryLogEntries.Enqueue(logEntry);
                }

                ampqConnection?.Close();
            }

            // If we enabled inter-batch delays, we don't loop indefinitely.
            if(_appConfig.SenderInterBatchDelayMs > 0) {
                break;
            }
        }
    }

    public async Task InitializeAsync(IDnsServer dnsServer, string config) {
        Console.WriteLine("QueryLogsAMQP: InitializeAsync START");

        _dnsServer = dnsServer;

        JsonDocument jsonDocument = JsonDocument.Parse(config);
        JsonElement jsonRootConfig = jsonDocument.RootElement;

        _appConfig.Enabled = jsonRootConfig.GetProperty("enabled").GetBoolean();

        _appConfig.AmqpHost = jsonRootConfig.GetProperty("amqpHost").GetString();
        _appConfig.AmqpPort = jsonRootConfig.GetProperty("amqpPort").GetInt16();
        _appConfig.AmqpVirtualHost = jsonRootConfig.GetProperty("amqpVirtualHost").GetString();
        _appConfig.AmqpRoutingKey = jsonRootConfig.GetProperty("amqpRoutingKey").GetString();
        _appConfig.AmqpExchangeName = jsonRootConfig.GetProperty("amqpExchangeName").GetString();

        _appConfig.AmqpAuthUsername = jsonRootConfig.GetProperty("amqpAuthUsername").GetString();
        _appConfig.AmqpAuthPassword = jsonRootConfig.GetProperty("amqpAuthPassword").GetString();
        _appConfig.AmqpHeartbeat = jsonRootConfig.GetProperty("amqpHeartbeat").GetInt32();
        _appConfig.AmqpReconnectInDispose = jsonRootConfig.GetProperty("amqpReconnectInDispose").GetBoolean();

        _appConfig.AmpqsEnabled = jsonRootConfig.GetProperty("ampqsEnabled").GetBoolean();
        _appConfig.AmpqsRequired = jsonRootConfig.GetProperty("ampqsRequired").GetBoolean();

        _appConfig.QueueMaxSize = jsonRootConfig.GetProperty("queueMaxSize").GetInt32();
        _appConfig.QueueMaxFailures = jsonRootConfig.GetProperty("queueMaxFailures").GetInt32();
        _appConfig.QueueFailuresBypassSizeLimits =
            jsonRootConfig.GetProperty("queueFailuresBypassSizeLimits").GetBoolean();

        _appConfig.SenderColdDelayMs = jsonRootConfig.GetProperty("senderColdDelayMs").GetInt32();
        _appConfig.SenderInterBatchDelayMs = jsonRootConfig.GetProperty("senderInterBatchDelayMs").GetInt32();
        _appConfig.SenderBatchMaxSize = jsonRootConfig.GetProperty("senderBatchMaxSize").GetInt32();
        _appConfig.SenderPostFailureDelayMs = jsonRootConfig.GetProperty("senderPostFailureDelayMs").GetInt32();

        if(_appConfig.Enabled) {
            _queueProcessingTimer.Change(5000, Timeout.Infinite);
        } else {
            _queueProcessingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        Console.WriteLine("QueryLogsAMQP: InitializeAsync END");
    }

    public void Dispose() {
        Console.WriteLine("QueryLogsAMQP: Dispose START");

        // Refusing any new entries in the queue.
        _appConfig.Enabled = false;

        // Stopping timers
        _queueProcessingTimer?.Dispose();

        // Flushing queues

        Console.WriteLine("QueryLogsAMQP: Dispose END");
    }

    public Task InsertLogAsync(DateTime timestamp, DnsDatagram request, IPEndPoint remoteEp,
        DnsTransportProtocol protocol, DnsDatagram response) {
        if(_appConfig.Enabled) {
            _queuedQueryLogEntries.Enqueue(new RawQueryLogEntry() {
                PushAttemptCount = 0,
                Timestamp = timestamp,
                Request = request,
                RemoteEp = remoteEp,
                Protocol = protocol,
                Response = response
            });
        }

        return Task.CompletedTask;
    }

    public Task<DnsLogPage> QueryLogsAsync(long pageNumber, int entriesPerPage, bool descendingOrder, DateTime? start,
        DateTime? end,
        IPAddress clientIpAddress, DnsTransportProtocol? protocol, DnsServerResponseType? responseType,
        DnsResponseCode? rcode, string qname, DnsResourceRecordType? qtype, DnsClass? qclass) {
        throw new NotImplementedException();
    }

    #region Private structs

    private struct AppConfig {
        public bool Enabled;

        public string AmqpHost;
        public short AmqpPort;
        public string AmqpVirtualHost;
        public string AmqpRoutingKey;
        public string AmqpExchangeName;
        public string AmqpAuthUsername;
        public string AmqpAuthPassword;
        public int AmqpHeartbeat;
        public bool AmqpReconnectInDispose;

        public bool AmpqsEnabled;
        public bool AmpqsRequired;

        public int QueueMaxSize;
        public int QueueMaxFailures;
        public bool QueueFailuresBypassSizeLimits;

        public int SenderColdDelayMs;
        public int SenderInterBatchDelayMs;
        public int SenderBatchMaxSize;
        public int SenderPostFailureDelayMs;
    }

    private struct RawQueryLogEntry {
        public int PushAttemptCount;
        public DateTime Timestamp;
        public DnsDatagram Request;
        public IPEndPoint RemoteEp;
        public DnsTransportProtocol Protocol;
        public DnsDatagram Response;
    }

    // ReSharper disable InconsistentNaming
    // ReSharper disable NotAccessedField.Local
    private struct ProcessedQueryLogEntry {
        public string timestamp;
        public string clientIp;
        public string protocol;
        public int responseType;
        public int rCode;
        public string qName;
        public int qType;
        public int qClass;
        public string answer;
    }

    #endregion

    #region App properties

    public string Description {
        get {
            // ReSharper disable once ArrangeAccessorOwnerBody
            return "Sends all incoming DNS requests and their responses to an AMQP broker. " +
                   "Query logs are queued internally and periodically sent to the given broker. " +
                   "[Failures ???]";
        }
    }

    #endregion
}
