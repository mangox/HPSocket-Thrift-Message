# HPSocket-Thrift-Message
C/S Framework based on HPSocket and Thrift.
这是一个基于[HPSocket](https://github.com/ldcsaa/HP-Socket)和[Thrift](http://thrift.apache.org/)开发的C/S架构的演示程序。

- 编写语言:C#
- .NET Framework: 4.5

**以下是大概开发流程**

- 网络框架HPSocket已经提供好，只需取过来稍作改造；
- 定义Thrift消息结构文件，具体类型可以参考Protocol.thrift里面的连接；
- 将thrift转化成类，集成到HPSocket网络框架传输过程中；
- 测试C/S之间收发消息


**将定义的thrift文件转化成C#类**

thrift-0.10.0.exe -gen csharp Protocol.thrift
