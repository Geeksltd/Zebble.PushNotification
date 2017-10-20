namespace Zebble.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Content;
    using Firebase.Iid;
    using Firebase.Messaging;
    using Java.IO;
    using Newtonsoft.Json.Linq;
    using Zebble;
    using Zebble.NativeImpl;
    using Newtonsoft.Json;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PushNotification : DevicePushNotification.INativeImplementation
    {
        static string SenderId => Config.Get("Push.Notification.Android.Sender.ID");

        public async Task DoRegister()
        {
            try
            {
                Firebase.FirebaseApp.InitializeApp(UIRuntime.CurrentActivity);
                var token = FirebaseInstanceId.Instance.Token;
                await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, token);
            }
            catch (Exception ex)
            {
                throw new Exception("Push-Notification registeration failed: " + ex);
            }
        }

        public Task DoUnregister() => Device.ThreadPool.Run(DoUnregisterOnThreadPool);

        async Task DoUnregisterOnThreadPool()
        {
            try
            {
                FirebaseInstanceId.Instance.DeleteToken(SenderId, FirebaseMessaging.InstanceIdScope);
                await Device.PushNotification.UnRegistered.RaiseOn(Device.ThreadPool);
            }
            catch (IOException ex)
            {
                await Device.PushNotification.ReceivedError.RaiseOn(Device.ThreadPool, "Failed to unregister Push Notification: " + ex);
            }
        }

        [Service(Exported = false)]
        [IntentFilter(new string[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
        public class RefreshService : FirebaseInstanceIdService
        {
            // Called if InstanceID token is updated. This may occur if the security of the previous token had been compromised. This call is initiated by the InstanceID provider.
            public override void OnTokenRefresh()
            {
                base.OnTokenRefresh();

                Device.ThreadPool.RunAction(async () =>
                {
                    try
                    {
                        var token = FirebaseInstanceId.Instance.Token;
                        await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, token);
                        Device.Log.Message("Refreshed token: " + token);
                    }
                    catch (Exception ex)
                    {
                        await Device.PushNotification.ReceivedError.RaiseOn(Device.ThreadPool,
                            "Failed to refresh the installation ID: " + ex);
                    }
                });
            }
        }

        [Service]
        [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
        public class PushNotificationGcmListener : FirebaseMessagingService
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
                    title  = notificationObject.Title,
                    titleLocalizationKey = notificationObject.TitleLocalizationKey
                };

                if (Device.PushNotification.ReceivedMessage.IsHandled())
                {
                    var values = JObject.Parse(JsonConvert.SerializeObject(notifyMessage));
                    var notification = new NotificationMessage(values);
                    Device.PushNotification.ReceivedMessage.RaiseOn(Device.ThreadPool, notification);
                }
                else
                {
                    var applicationName = UIRuntime.CurrentActivity.ApplicationInfo.LoadLabel(UIRuntime.CurrentActivity.PackageManager);
                    Device.LocalNotification.Show(applicationName, message.GetNotification().Body);
                }
            }
        }

        public async Task<bool> OnMessageReceived(object message) { return true; }

        public async Task OnRegisteredSuccess(object token) { }

        public async Task OnUnregisteredSuccess() { }

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