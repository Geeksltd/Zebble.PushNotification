namespace Zebble.Device
{
    using Android.App;
    using Android.Content;
    using Context = Android.Content.Context;
    using Android.Gms.Extensions;
    using Firebase.Iid;
    using Firebase.Messaging;
    using Java.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Olive;

    [EditorBrowsable(EditorBrowsableState.Never)]
    partial class PushNotification
    {
        static string SenderId => Config.Get("Push.Notification.Android.Sender.ID");

        static void Init() { }

        static Task DoRegister()
        {
            try
            {
                Firebase.FirebaseApp.InitializeApp(UIRuntime.CurrentActivity);
                FirebaseInstanceId.Instance.GetInstanceId()
                    .AddOnCompleteListener(UIRuntime.CurrentActivity, new OnCompleteListener());

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new Exception("Push-Notification registration failed: " + ex);
            }
        }

        static Task DoUnRegister(object userState) => Thread.Pool.Run(() => DoUnRegisterOnThreadPool(userState));

        static async Task DoUnRegisterOnThreadPool(object userState)
        {
            try
            {
                FirebaseInstanceId.Instance.DeleteToken(SenderId, FirebaseMessaging.InstanceIdScope);
                await UnRegistered.RaiseOn(Thread.Pool, userState);
            }
            catch (IOException ex)
            {
                await ReceivedError.RaiseOn(Thread.Pool, "Failed to un-register PushNotification: " + ex);
            }
        }

        internal class OnCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
        {
            public async void OnComplete(Android.Gms.Tasks.Task task)
            {
                if (!task.IsSuccessful)
                {
                    Log.For(this).Error("PushNotification retrieving token was not successful!");
                    return;
                }

                var currentTask = await task.AsAsync<IInstanceIdResult>();
                if (currentTask != null)
                    await Registered.RaiseOn(Thread.Pool, currentTask.Token);
            }
        }

        [Service]
        [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
        internal class RefreshService : FirebaseMessagingService
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