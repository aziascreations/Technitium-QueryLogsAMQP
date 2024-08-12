namespace QueryLogsAMQP.Tests;

// ReSharper disable SuggestVarOrType_BuiltInTypes
public class TestInfiniteRequeuing {
    private App _dnsApp;
    
    [SetUp]
    public void Setup() {
        _dnsApp = new App();
        _dnsApp.DnsAppConfig.QueueMaxFailures = -1;
        
        // We only changed this parameter from the non-infinite variant
        _dnsApp.DnsAppConfig.QueueMaxSize = -1;
    }

    [Test]
    public void TestStrictest() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = false;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = false;
        
        // Filling 9 of the 10 available slots
        for(int i = 0; i < _dnsApp.DnsAppConfig.QueueMaxSize - 1; i++) {
            _dnsApp.RequeueLogEntry(new App.RawQueryLogEntry());
        }
        
        // Attempting to fill the 10th slot
        Assert.That(_dnsApp.RequeueLogEntry(new App.RawQueryLogEntry()), Is.True);
        
        // Attempting to fill the 11th slot
        // Shouldn't fail since the queue has no limit.
        Assert.That(_dnsApp.RequeueLogEntry(new App.RawQueryLogEntry()), Is.True);
    }

    [Test]
    public void TestEjectOldest() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = false;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = true;
        
        // Filling 9 of the 10 available slots
        for(int i = 0; i < 9; i++) {
            _dnsApp.RequeueLogEntry(new App.RawQueryLogEntry());
        }
        
        // Attempting to fill the 10th and 11th slots
        Assert.That(_dnsApp.RequeueLogEntry(new App.RawQueryLogEntry()), Is.True);
        Assert.That(_dnsApp.RequeueLogEntry(new App.RawQueryLogEntry()), Is.True);
        
        // We shouldn't have lost any either
        Assert.That(_dnsApp.QueuedQueryLogEntries, Has.Count.EqualTo(11));
    }

    [Test]
    public void TestDefaultWithSingleBypass() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = true;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = false;
        
        // We shouldn't be dropping any log either
        int desiredLogCount = _dnsApp.DnsAppConfig.QueueMaxSize + 25;
        for(int i = 0; i < desiredLogCount; i++) {
            Assert.That(_dnsApp.RequeueLogEntry(new App.RawQueryLogEntry()), Is.True);
        }
        Assert.That(_dnsApp.QueuedQueryLogEntries.Count, Is.EqualTo(desiredLogCount));
    }

    [Test]
    public void TestDefaultWithDoubleBypass() {
        // Re-queuing parameters
        _dnsApp.DnsAppConfig.QueueFailuresBypassSizeLimits = true;
        _dnsApp.DnsAppConfig.QueueFailuresEjectOldestOnRequeue = true;
        
        // We shouldn't be dropping any log either
        int desiredLogCount = _dnsApp.DnsAppConfig.QueueMaxSize + 25;
        for(int i = 0; i < desiredLogCount; i++) {
            Assert.That(_dnsApp.RequeueLogEntry(new App.RawQueryLogEntry()), Is.True);
        }
        Assert.That(_dnsApp.QueuedQueryLogEntries.Count, Is.EqualTo(desiredLogCount));
    }

    [TearDown]
    public void TearDown() {
        _dnsApp.Dispose();
    }
}
