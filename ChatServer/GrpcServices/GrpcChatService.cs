using Chat;
using ChatServer.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace ChatServer.GrpcServices;

public class GrpcChatService: ChatService.ChatServiceBase
{
    private readonly IChatDataProvider _chatDataProvider;

    public GrpcChatService(IChatDataProvider chatDataProvider)
    {
        _chatDataProvider = chatDataProvider;
    }
    
    public override Task<Empty> Connect(ConnectRequest request, ServerCallContext context)
    {
        _chatDataProvider.ConnectUser(request.UserId);
        return Task.FromResult(new Empty());
    }
    
    public override Task<Empty> Disconnect(DisconnectRequest request, ServerCallContext context)
    {
        _chatDataProvider.DisconnectUser(request.UserId);
        return Task.FromResult(new Empty());
    }
    
    public override async Task SendChatMessage(IAsyncStreamReader<ChatMessageRequest> requestStream, IServerStreamWriter<ChatMessageResponse> responseStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext())
            return;
    
        var senderId = requestStream.Current.SenderId;
        _chatDataProvider.SaveUserInfo(senderId, responseStream);
    
        do
        {
            var chatMessage = requestStream.Current.Message;
            if (string.IsNullOrEmpty(chatMessage))
                continue;
    
            if (string.Equals(chatMessage, "qw!", StringComparison.OrdinalIgnoreCase))
                break;
    
            _chatDataProvider.SendAsync(requestStream.Current.ReceiverId, chatMessage, senderId);
        } while (await requestStream.MoveNext());
    }
}