# AMQP Query Logging Technitium App
A simple application for [Technitium DNS](https://github.com/TechnitiumSoftware/DnsServer)
that logs queries to an AMQP broker using
[RabbitMQ's client library](https://github.com/rabbitmq/rabbitmq-dotnet-client).

<!--
## Features
* Support for AMQP & ~~AMQPS~~
* ???
-->

<!--
## Summary
* [Installation](#installation)
* [Configuration](#configuration)
  * [AMQP Connection](#amqp-connection)
  * [AMQP Messages](#amqp-messages)
  * [Internal Log Queue](#internal-log-queue)
  * [AMQP Client Task](#amqp-client-task)
* [Building](#building)
  * [Cloning Git Submodules](#cloning-git-submodules)
  * [Building using script](#building-using-scripts)
* [Licenses](#licenses)
-->


## Installation
1. Download the latest `.zip` file from the
[releases page](https://github.com/aziascreations/Technitium-QueryLogsAMQP/releases).
2. Login into your Technitium administration panel and go into the *"Apps"* tab.<br>
   <image src="Documentation/technitium-install-1.png" width="422px">
3. Click on the *"Install" button.*<br>
   <image src="Documentation/technitium-install-2.png" width="367px">
4. Give the application a name, select the downloaded `.zip` file and click on install.<br>
   <image src="Documentation/technitium-install-3.png" width="226px">
5. Configure the application by following the [Configuration section](#Configuration).


## Configuration
This application can be made to suit a variety of situations and log throughput via Technitium's administration panel.

1. Go into the *"Apps"* tab, find the installed application and click on *""*.
2. Make changes to the configuration inside the modal.
3. Any error should be reported ???.

<details>
<summary>Click here to see an example configuration</summary>

```json
{
    "enabled": true,
    
    "amqpHost": "127.0.0.1",
    "amqpPort": 5672,
    "amqpVirtualHost": "/technitium",
    "amqpAuthUsername": "technitium-na1",
    "amqpAuthPassword": "change-me",
    "amqpRoutingKey": "dns-na1",
    "amqpExchangeName": "amq.topic",
    "amqpHeartbeat": 30,
    "amqpReconnectInDispose": true,
    
    "ampqsEnabled": false,
    "ampqsRequired": true,
    
    "queueMaxSize": 10000,
    "queueMaxFailures": 5,
    "queueFailuresBypassSizeLimits": true,
    
    "senderColdDelayMs": 5000,
    "senderInterBatchDelayMs": 500,
    "senderBatchMaxSize": 100,
    "senderPostFailureDelayMs": 250
}
```

</details>


### AMQP Connection
| Field                           | Description                                    |
|---------------------------------|------------------------------------------------|
| `enabled`                       | Allows or prevent the app from processing logs |
| `amqpHost`                      | Broker's [???]                                 |
| `amqpPort`                      | Broker's exposed port                          |
| `amqpVirtualhost`               | Broker's virtual host                          |
| `amqpAuthUsername`              | Username with which to connect to the broker   |
| `amqpAuthPassword`              | Password with which to connect to the broker   |
| `amqpHeartbeat`                 | ~~Not Implemented Yet~~                        |
| `amqpReconnectInDispose`        | ~~Not Implemented Yet~~                        |
| `ampqsEnabled`                  | ~~Not Implemented Yet~~                        |
| `ampqsRequired`                 | ~~Not Implemented Yet~~                        |


### AMQP Messages
| Field                           | Description                                |
|---------------------------------|--------------------------------------------|
| `amqpRoutingKey`                | Routing key used by every message          |
| `amqpExchangeName`              | Exchange's name to which messages are sent |


### Internal Log Queue
| Field                           | Description                     |
|---------------------------------|---------------------------------|
| `queueMaxSize`                  | ~~Not Implemented Yet~~         |
| `queueMaxFailures`              | ~~Not Implemented Yet~~         |
| `queueFailuresBypassSizeLimits` | ~~Not Implemented Yet~~         |


### AMQP Client Task
| Field                           | Description                     |
|---------------------------------|---------------------------------|
| `senderColdDelayMs`             | ~~Not Implemented Yet~~         |
| `senderInterBatchDelayMs`       | ~~Not Implemented Yet~~         |
| `senderBatchMaxSize`            | ~~Not Implemented Yet~~         | 
| `senderPostFailureDelayMs`      | ~~Not Implemented Yet~~         |


## Building
This sections assumes you already have *git* and *dotnet* installed.

### Cloning Git Submodules
If you're cloning the repository for the first time:<br>
`git clone --recurse-submodules https://github.com/aziascreations/Technitium-QueryLogsAMQP.git`

If you cloned the repository with the submodules:<br>
`git submodule update --init`


### Building using scripts
The application can be built by using the [build.cmd](build.cmd) script.

If you don't want to use it or are on Linux, you can simply run the appropriate
commands present in that script.


## Licenses
The code in this repository is licensed under the [GNU GPLv3](LICENSE).

### External licenses
* RabbitMQ.Client - [Apache-2.0 license](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/LICENSE-APACHE2)
* Newtonsoft.Json - [MIT License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
