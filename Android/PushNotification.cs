namespace Zebble.Device
{
    using Android.App;
    using Android.Content;
    using Firebase.Iid;
    using Firebase.Messaging;
    using Java.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    [EditorBrowsable(EditorBrowsableState.Never)]
    partial class PushNotification
    {
        static string SenderId => Config.Get("Push.Notification.Android.Sender.ID");

        static void Init() { }

        static async Task DoRegister()
        {
            try
            {
                Firebase.FirebaseApp.InitializeApp(UIRuntime.CurrentActivity);
                var token = FirebaseInstanceId.Instance.Token;
                await Registered.RaiseOn(Thread.Pool, token);
            }
            catch (Exception ex)
            {
                throw new Exception("Push-Notification registeration failed: " + ex);
            }
        }

        static Task DoUnregister() => Thread.Pool.Run(DoUnregisterOnThreadPool);

        static async Task DoUnregisterOnThreadPool()
        {
            try
            {
                FirebaseInstanceId.Instance.DeleteToken(SenderId, FirebaseMessaging.InstanceIdScope);
                await UnRegistered.RaiseOn(Thread.Pool);
            }
            catch (IOException ex)
            {
                await ReceivedError.RaiseOn(Thread.Pool, "Failed to unregister Push Notification: " + ex);
            }
        }

        [Service(Exported = false)]
        [IntentFilter(new string[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
        internal class RefreshService : FirebaseInstanceIdService
        {
            // Called if InstanceID token is updated. This may occur if the security of the previous token had been compromised. This call is initiated by the InstanceID provider.
            public override void OnTokenRefresh()
            {
                base.OnTokenRefresh();

                Thread.Pool.RunAction(async () =>
                {
                    try
                    {
                        var token = FirebaseInstanceId.Instance.Token;
                        await Registered.RaiseOn(Thread.Pool, token);
                        Device.Log.Message("Refreshed token: " + token);
                    }
                    catch (Exception ex)
                    {
                        await ReceivedError.RaiseOn(Thread.Pool,
                            "Failed to refresh the installation ID: " + ex);
                    }
                });
            }
        }

        [Service]
        [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
        internal class PushNotificationGcmListener : FirebaseMessagingService
        {
            static Context Context => UIRuntime.CurrentActivity;
            public override void OnMessageReceived(RemoteMessage message)
            {
                var notificationObject = message.GetNotification();
                var notifyMessage = new AndroidNotificationMessage
                {
                    body = notificationObject.Body,
                    bodyLocalizationKey = notificationObject.BodyLocalizationKey,
                    clickAction = notificationObject.ClickAction,
                    color = notificationObject.Color,
                    icon = notificationObject.Icon,
                    link = notificationObject.Link,
                    sound = notificationObject.Sound,
                    tag = notificationObject.Tag,
                    title = notificationObject.Title,
                    titleLocalizationKey = notificationObject.TitleLocalizationKey
                };

                if (ReceivedMessage.IsHandled())
                {
                    var values = JObject.Parse(JsonConvert.SerializeObject(notifyMessage));
                    var notification = new Zebble.Device.LocalNotification.Notification
                    {
                        Title = notificationObject.Title,
                        Body = notificationObject.Body,
                        NotifyTime = LocalTime.Now,
                    };

                    ReceivedMessage.RaiseOn(Thread.Pool, notification);
                }
                else
                {
                    var applicationName = UIRuntime.CurrentActivity.ApplicationInfo.LoadLabel(UIRuntime.CurrentActivity.PackageManager);
                    LocalNotification.Show(applicationName, message.GetNotification().Body);
                }
            }
        }

        static async Task<bool> OnMessageReceived(object message) { return true; }

        static async Task OnRegisteredSuccess(object token) { }

        static async Task OnUnregisteredSuccess() { }

        [EscapeGCop("LowerCase property name is ok.")]
        internal class AndroidNotificationMessage
        {
            public string body { get; set; }
            public string bodyLocalizationKey { get; set; }
            public string clickAction { get; set; }
            public string color { get; set; }
            public string icon { get; set; }
            public Android.Net.Uri link { get; set; }
            public string sound { get; set; }
            public string tag { get; set; }
            public string title { get; set; }
            public string titleLocalizationKey { get; set; }
        }
    }
}