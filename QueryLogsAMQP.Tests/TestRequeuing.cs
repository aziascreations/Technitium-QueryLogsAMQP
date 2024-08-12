namespace QueryLogsAMQP.Tests;

// ReSharper disable SuggestVarOrType_BuiltInTypes
public class TestRequeuing {
    private App _dnsApp;
    
    [SetUp]
    public void Setup() {
        _dnsApp = new App();
        _dnsApp.DnsAppConfig.QueueMaxFailures = -1;
        _dnsApp.DnsAppConfig.QueueMaxSize = 10;
    }

    [Test]
    public void TestStrictest() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = false;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = false;
        
        // Filling 9 of the 10 available slots
        for(int i = 0; i < _dnsApp.DnsAppConfig.QueueMaxSize - 1; i++) {
            _dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry());
        }
        
        // Attempting to fill the 10th slot
        Assert.That(_dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry()), Is.True);
        
        // Attempting to fill the 11th slot
        // Should fail since `QueueFailuresEjectOldestOnRequeue` is `false`.
        Assert.That(_dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry()), Is.False);
    }

    [Test]
    public void TestEjectOldest() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = false;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = true;
        
        // Filling 9 of the 10 available slots
        for(int i = 0; i < _dnsApp.DnsAppConfig.QueueMaxSize - 1; i++) {
            _dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry());
        }
        
        // Attempting to fill the 10th and 11th slots
        Assert.That(_dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry()), Is.True);
        Assert.That(_dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry()), Is.True);
        
        // We should have lost one in the process
        Assert.That(_dnsApp.DnsAppConfig.QueueMaxSize, Is.EqualTo(_dnsApp.QueuedQueryLogEntries.Count));
    }

    [Test]
    public void TestDefaultWithSingleBypass() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = true;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = false;
        
        // Filling more slots than what should be possible uf not bypass rule was set up.
        int desiredLogCount = _dnsApp.DnsAppConfig.QueueMaxSize + 10;
        for(int i = 0; i < desiredLogCount; i++) {
            Assert.That(_dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry()), Is.True);
        }
        Assert.That(_dnsApp.QueuedQueryLogEntries, Has.Count.EqualTo(desiredLogCount));
    }

    [Test]
    public void TestDefaultWithDoubleBypass() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = true;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = true;
        
        // Filling more slots than what should be possible uf not bypass rule was set up.
        // We shouldn't be dropping any log either
        int desiredLogCount = _dnsApp.DnsAppConfig.QueueMaxSize + 10;
        for(int i = 0; i < desiredLogCount; i++) {
            Assert.That(_dnsApp.RequeueFailedLogEntry(new App.RawQueryLogEntry()), Is.True);
        }
        Assert.That(_dnsApp.QueuedQueryLogEntries, Has.Count.EqualTo(desiredLogCount));
    }

    [TearDown]
    public void TearDown() {
        _dnsApp.Dispose();
    }
}
