namespace Zebble.Device
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Windows.Networking.PushNotifications;
    using Olive;

    [EditorBrowsable(EditorBrowsableState.Never)]
    partial class PushNotification
    {
        static PushNotificationChannel Channel;
        static readonly JsonSerializer Serializer = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        static void Init() { }

        static Task<bool> OnMessageReceived(object message)
        {
            static async void Report(object data)
            {
                var values = JObject.FromObject(data, Serializer);
                if (ReceivedMessage.IsHandled())
                {
                    var notification = new LocalNotification.Notification
                    {
                        Title = values["title"].ToString(),
                        Body = values["body"].ToString(),
                        NotifyTime = LocalTime.Now,
                    };

                    await ReceivedMessage.RaiseOn(Thread.Pool, notification);
                    // TODO: How to increase the badge number?
                }
                else
                {
                    var applicationName = Windows.ApplicationModel.Package.Current.DisplayName;
                    await LocalNotification.Show(applicationName, values["body"].Value<string>());
                }
            };

            var args = message as PushNotificationReceivedEventArgs;

            switch (args.NotificationType)
            {
                case PushNotificationType.Raw: Report(args.RawNotification); break;
                case PushNotificationType.Badge: Report(args.BadgeNotification); break;
                case PushNotificationType.Toast: Report(args.ToastNotification); break;

                case PushNotificationType.Tile:
                case PushNotificationType.TileFlyout:
                    Report(args.TileNotification); break;
            }

            return Task.FromResult(result: true);
        }

        static async Task DoRegister()
        {
            Channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync()
                .AsTask();

            Channel.PushNotificationReceived += OnReceived;

            await Registered.RaiseOn(Thread.Pool, Channel?.Uri);
        }

        static Task DoUnRegister(object userState)
        {
            if (Channel != null)
            {
                Channel.PushNotificationReceived -= OnReceived;
                Channel = null;
            }

            return UnRegistered.RaiseOn(Thread.Pool, userState);
        }

        static void OnReceived(PushNotificationChannel _, PushNotificationReceivedEventArgs args) => OnMessageReceived(args);

        static Task OnRegisteredSuccess(object token) => Task.CompletedTask;

        static Task OnUnRegisteredSuccess() => Task.CompletedTask;
    }
}