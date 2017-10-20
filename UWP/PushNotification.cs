namespace Zebble.Plugin
{
    using System;
    using System.ComponentModel;
    using Zebble;
    using System.Threading.Tasks;
    using Windows.Networking.PushNotifications;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Zebble.NativeImpl;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PushNotification : DevicePushNotification.INativeImplementation
    {
        PushNotificationChannel Channel;
        readonly JsonSerializer Serializer = new JsonSerializer { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        public Task<bool> OnMessageReceived(object message)
        {
            async void report(object data)
            {
                var values = JObject.FromObject(data, Serializer);
                if (Device.PushNotification.ReceivedMessage.IsHandled())
                {
                    var notifcation = new NotificationMessage(values);
                    await Device.PushNotification.ReceivedMessage.RaiseOn(Device.ThreadPool, notifcation);

                    // TODO: How to increase the badge number?
                }
                else
                {
                    var applicationName = Windows.ApplicationModel.Package.Current.DisplayName;
                    await Device.LocalNotification.Show(applicationName, values["body"].Value<string>());
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

        public async Task DoRegister()
        {
            Channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync()
                .AsTask();

            Channel.PushNotificationReceived += OnReceived;

            await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, Channel?.Uri);
        }

        public Task DoUnregister()
        {
            if (Channel != null)
            {
                Channel.PushNotificationReceived -= OnReceived;
                Channel = null;
            }

            return Device.PushNotification.UnRegistered.RaiseOn(Device.ThreadPool);
        }

        void OnReceived(PushNotificationChannel _, PushNotificationReceivedEventArgs args) => OnMessageReceived(args);

        public async Task OnRegisteredSuccess(object token) { }

        public async Task OnUnregisteredSuccess() { }
    }
}