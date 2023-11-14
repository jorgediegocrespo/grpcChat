using Chat;
using Grpc.Core;

namespace ChatServer.Services;

public interface IChatDataProvider
{
    void ConnectUser(string userId);
    void SaveUserInfo(string userId, IServerStreamWriter<ChatMessageResponse> CustomerResponseStream);
    void SendAsync(string receiverId, string message, string senderId);
    void DisconnectUser(string userId);
}