using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using DnsServerCore.ApplicationCommon;
using TechnitiumLibrary.Net.Dns;
using TechnitiumLibrary.Net.Dns.ResourceRecords;

namespace QueryLogsAMQP;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable once UnusedType.Global
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
public sealed class App : IDnsApplication, IDnsQueryLogger {
    /* Instance variables */
    private IDnsServer _dnsServer;
    private AppConfig _appConfig;
    
    private Connection _amqpConnection;
    
    
    
    public App() {
        
    }

    private async Task ForceCloseConnection() {
        try {
            if(_amqpConnection != null) {
                await _amqpConnection.CloseAsync(new TimeSpan(0, 0, 0, 0, 1));
            }
        } catch(Exception) {
            // We can just ignore those since we want to reconnect.
        } finally {
            _amqpConnection = null;
        }
    }
    
    private async Task OpenConnection() {
        await ForceCloseConnection();

        try {
            _amqpConnection = await Connection.Factory.CreateAsync(
                new Address(
                    host: _appConfig.AmqpHost,
                    port: _appConfig.AmqpPort,
                    user: _appConfig.AmqpAuthUsername,
                    password: _appConfig.AmqpAuthPassword,
                    path: _appConfig.AmqpVirtualHost,
                    scheme: "ampq"
                )
            );
        } catch(Exception e) {
            Console.Error.Write(e);
        }
    }
    
    public async Task InitializeAsync(IDnsServer dnsServer, string config) {
        _dnsServer = dnsServer;
        
        JsonDocument jsonDocument = JsonDocument.Parse(config);
        JsonElement jsonRootConfig = jsonDocument.RootElement;
        
        _appConfig.Enabled = jsonRootConfig.GetProperty("enabled").GetBoolean();
        
        _appConfig.AmqpHost = jsonRootConfig.GetProperty("amqpHost").GetString();
        _appConfig.AmqpPort = jsonRootConfig.GetProperty("amqpPort").GetInt16();
        _appConfig.AmqpVirtualHost = jsonRootConfig.GetProperty("amqpVirtualHost").GetString();
        _appConfig.AmqpRoutingKey = jsonRootConfig.GetProperty("amqpRoutingKey").GetString();
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
        
        await OpenConnection();
    }
    
    public void Dispose() {
        // Refusing any new entries in the queue.
        _appConfig.Enabled = false;
        
        
        // Flushing queues
        
        // Killing the connection
        _ = ForceCloseConnection();
    }

    public Task InsertLogAsync(DateTime timestamp, DnsDatagram request, IPEndPoint remoteEP, DnsTransportProtocol protocol,
        DnsDatagram response) {
        throw new NotImplementedException();
    }

    public Task<DnsLogPage> QueryLogsAsync(long pageNumber, int entriesPerPage, bool descendingOrder, DateTime? start, DateTime? end,
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
