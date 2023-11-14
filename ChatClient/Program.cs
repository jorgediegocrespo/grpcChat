using Chat;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace ChatClient
{
    internal class Program
    {
        private static GrpcChannel channel;
        private static ChatService.ChatServiceClient chatClient;
        private static AsyncDuplexStreamingCall<ChatMessageRequest, ChatMessageResponse> sendChatMessageStream;
        private static string senderId;
        private static string receiverId;
        
        static async Task Main(string[] args)
        {
            SetupChannel();
            await ConnectAsync();
            await ProcessReceivedMessagesAsync();

            if (await SendMessageAsync()) 
                return;
            
            await DisconnectAsync();
        }

        private static void SetupChannel()
        {
            var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            });
            
            var handler = new SocketsHttpHandler
            {
                KeepAlivePingDelay = TimeSpan.FromSeconds(15), //Ping to server to keep the connection alive
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5), //Timeout for the ping to avoid pings flood the server
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(10),
                UseProxy = false,
            };
            
            channel = GrpcChannel.ForAddress("http://localhost:5002", new GrpcChannelOptions
            {
                LoggerFactory = loggerFactory,
                Credentials = ChannelCredentials.Insecure,
                HttpHandler = handler
            });

            chatClient = new ChatService.ChatServiceClient(channel);
        }
        
        private static async Task ConnectAsync()
        {
            senderId = GetUserId();
            receiverId = GetReceiverId();
            ConnectRequest connectRequest = new ConnectRequest { UserId = senderId };
            AsyncUnaryCall<Empty> connectCall = chatClient.ConnectAsync(connectRequest);
            await connectCall.ResponseAsync;
        }
        
        private static string GetUserId()
        {
            Console.WriteLine("Please enter your name:");
            string userId = Console.ReadLine();
            while (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Please enter a valid name. Cannot be null or empty.");
                userId = Console.ReadLine();
            }

            return userId;
        }
        
        private static string GetReceiverId()
        {
            Console.WriteLine("Please enter the name of the person you want to talk to:");
            string userId = Console.ReadLine();
            while (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Please enter a valid name. Cannot be null or empty.");
                userId = Console.ReadLine();
            }

            return userId;
        }
        
        private static async Task ProcessReceivedMessagesAsync()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            sendChatMessageStream = chatClient.SendChatMessage();
            await sendChatMessageStream.RequestStream.WriteAsync(new ChatMessageRequest
            {
                SenderId = senderId,
                ReceiverId = string.Empty,
                Message = string.Empty,
            });
            
            _ = Task.Run(async () =>
            {
                // Here are process all the message the customer received from the server
                while (await sendChatMessageStream.ResponseStream.MoveNext())
                    ShowReceivedMessage(sendChatMessageStream.ResponseStream.Current);
            });
        }
        
        private static void ShowReceivedMessage(ChatMessageResponse chatMessage)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{chatMessage.SenderId}: {chatMessage.Message}");
            Console.ForegroundColor = ConsoleColor.Green;
        }

        private static async Task<bool> SendMessageAsync()
        {
            var message = Console.ReadLine();
            ShowSentMessage(message);
            while (!string.Equals(message, "qw!", StringComparison.OrdinalIgnoreCase))
            {
                await SendMessageAsync(message);
                message = Console.ReadLine();
                ShowSentMessage(message);
            }

            return false;
        }
        
        private static async Task SendMessageAsync(string message)
        {
            await sendChatMessageStream.RequestStream.WriteAsync(new ChatMessageRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
            });
        }
        
        private static void ShowSentMessage(string message)
        {
            if (Console.CursorTop == 0) 
                return;
            
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            
            if (!string.IsNullOrEmpty(message))
                Console.WriteLine($"You: {message}");
        }

        private static async Task DisconnectAsync()
        {
            await sendChatMessageStream.RequestStream.CompleteAsync();
            AsyncUnaryCall<Empty> disconnectCall = chatClient.DisconnectAsync(new DisconnectRequest { UserId = senderId });
            await disconnectCall.ResponseAsync;
            Console.ReadKey();

            channel.Dispose();
            await channel.ShutdownAsync();
        }
    }
}
