namespace Zebble.Device
{
    using Android.App;
    using Android.Content;

    using Android.Gms.Extensions;
    using Firebase.Iid;
    using Firebase.Messaging;
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Olive;

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
            }
            catch (Exception ex)
            {
                await ReceivedError.RaiseOn(Thread.Pool, "Failed to register PushNotification: " + ex);
            }
        }

        [Service(Exported = false)]
        [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
        public class RefreshService : FirebaseMessagingService
        {
            public override void OnNewToken(string p0)
            {
                base.OnNewToken(p0);

                Thread.Pool.RunAction(async () =>
                {
                    try
                    {
                        var token = p0;
                        await Registered.RaiseOn(Thread.Pool, token);
                        Log.For(this).Debug("Refreshed token: " + token);
                    }
                    catch (Exception ex)
                    {
                        await ReceivedError.RaiseOn(Thread.Pool,
                            "Failed to refresh the installation ID: " + ex);
                    }
                });
            }
        }

        [Service(Exported = false)]
        [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
        public class PushNotificationGcmListener : FirebaseMessagingService
        {
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
                    var notification = new LocalNotification.Notification
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
                    LocalNotification.Show(applicationName, notificationObject.Body);
                }
            }
        }

        static async Task<bool> OnMessageReceived(object message) { return true; }

        static async Task OnRegisteredSuccess(object token) { }

        static async Task OnUnRegisteredSuccess() { }

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