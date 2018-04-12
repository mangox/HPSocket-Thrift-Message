namespace csharp MessageProtocol

# http://thrift.apache.org/docs/types

# �����
struct RequestTask {
	1: string clientId
	# client�汾
	2: string version
	3: string message
}

# �����
struct ResultModel {
	1: string clientId
	2: string projectId
	# ��������ʱ��resultΪ������쳣������Ϊ�쳣��Ϣ
	3: string result
	# true����ȷ������� false: �쳣����
	4: bool status
}

# �����
struct TaskModel {
	1: string clientId
	2: string projectId
	3: string taskId
}