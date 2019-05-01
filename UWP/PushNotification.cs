namespace Zebble.Device
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Windows.Networking.PushNotifications;

    [EditorBrowsable(EditorBrowsableState.Never)]
    partial class PushNotification
    {
        static PushNotificationChannel Channel;
        readonly static JsonSerializer Serializer = new JsonSerializer { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        static void Init() { }

        static Task<bool> OnMessageReceived(object message)
        {
            async void report(object data)
            {
                var values = JObject.FromObject(data, Serializer);
                if (ReceivedMessage.IsHandled())
                {
                    var notification = new Zebble.Device.LocalNotification.Notification
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
                case PushNotificationType.Raw: report(args.RawNotification); break;
                case PushNotificationType.Badge: report(args.BadgeNotification); break;
                case PushNotificationType.Toast: report(args.ToastNotification); break;

                case PushNotificationType.Tile:
                case PushNotificationType.TileFlyout:
                    report(args.TileNotification); break;

                default: break;
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

        static Task DoUnregister()
        {
            if (Channel != null)
            {
                Channel.PushNotificationReceived -= OnReceived;
                Channel = null;
            }

            return UnRegistered.RaiseOn(Thread.Pool);
        }

        static void OnReceived(PushNotificationChannel _, PushNotificationReceivedEventArgs args) => OnMessageReceived(args);

        static async Task OnRegisteredSuccess(object token) { }

        static async Task OnUnregisteredSuccess() { }
    }
}