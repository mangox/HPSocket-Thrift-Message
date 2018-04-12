namespace csharp MessageProtocol

# http://thrift.apache.org/docs/types

# 请求包
struct RequestTask {
	1: string clientId
	# client版本
	2: string version
	3: string message
}

# 结果包
struct ResultModel {
	1: string clientId
	2: string projectId
	# 正常结束时，result为结果，异常结束下为异常信息
	3: string result
	# true：正确计算结束 false: 异常结束
	4: bool status
}

# 任务包
struct TaskModel {
	1: string clientId
	2: string projectId
	3: string taskId
}