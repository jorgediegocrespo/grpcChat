using Chat;
using Grpc.Core;

namespace ChatServer.Services;

internal class ChatDataProvider : IChatDataProvider
{
    private readonly Dictionary<string, IServerStreamWriter<ChatMessageResponse>> _streamDic = new();

    public void ConnectUser(string userId)
    {
        if (_streamDic.ContainsKey(userId))
            return;
            
        _streamDic.Add(userId, null);
    }
        
    public void SaveUserInfo(string userId, IServerStreamWriter<ChatMessageResponse> CustomerResponseStream)
    {
        if (!_streamDic.ContainsKey(userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Could not find user."));
            
        _streamDic[userId] = CustomerResponseStream;
    }

    public async void SendAsync(string receiverId, string message, string senderId)
    {
        if (!_streamDic.TryGetValue(receiverId, out var stream))
            return;

        if (stream == null)
            return;
            
        await stream.WriteAsync(new ChatMessageResponse
        {
            Message = message,
            SenderId = senderId
        });
    }

    public void DisconnectUser(string userId)
    {
        if (_streamDic.ContainsKey(userId))
            _streamDic.Remove(userId);
    }
}