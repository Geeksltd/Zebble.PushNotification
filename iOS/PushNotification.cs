namespace Zebble.Plugin
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json.Linq;
    using Zebble;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;
    using Zebble.NativeImpl;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PushNotification : DevicePushNotification.INativeImplementation
    {
        public Task DoUnregister()
        {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            return OnUnregisteredSuccess();
        }

        public Task DoRegister()
        {
            var app = UIApplication.SharedApplication;

            if (Device.OS.IsAtLeastiOS(8, 0))
            {
                app.RegisterUserNotificationSettings(
                    UIUserNotificationSettings.GetSettingsForTypes(
                        UIUserNotificationType.Alert |
                        UIUserNotificationType.Badge |
                        UIUserNotificationType.Sound, null));
                app.RegisterForRemoteNotifications();
            }
            else
            {
                app.RegisterForRemoteNotificationTypes(
                        UIRemoteNotificationType.Alert |
                        UIRemoteNotificationType.Badge |
                        UIRemoteNotificationType.Sound);
            }

            return Task.CompletedTask;
        }

        public async Task<bool> OnMessageReceived(object message)
        {
            var userInfo = message as NSDictionary;

            var json = DictionaryToJson(userInfo);
            var values = new JObject();

            var keyAps = new NSString("aps");
            var keyAlert = new NSString("alert");

            if (userInfo.ContainsKey(keyAps))
            {
                var aps = userInfo.ValueForKey(keyAps) as NSDictionary;

                if (aps != null)
                {
                    var alert = aps.ValueForKey(keyAlert) as NSDictionary;

                    if (alert != null)
                        foreach (var node in alert)
                        {
                            JToken temp;
                            if (!values.TryGetValue(node.Key.ToString(), out temp))
                                values.Add(node.Key.ToString(), node.Value.ToString());
                        }
                }
            }

            var notification = new NotificationMessage(values);
            await Device.PushNotification.ReceivedMessage.RaiseOn(Device.ThreadPool, notification);
            return notification.ReceivedNewData;
        }

        public async Task OnRegisteredSuccess(object token)
        {
            var cleanToken = (token as NSData)?.Description.OrEmpty().Trim().Remove(" ").Trim('<', '>').Trim();
            await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, cleanToken);
            SetUserDefault(cleanToken);
        }

        public async Task OnUnregisteredSuccess()
        {
            await Device.PushNotification.UnRegistered.RaiseOn(Device.ThreadPool);
            SetUserDefault(string.Empty);
        }

        void SetUserDefault(string token)
        {
            NSUserDefaults.StandardUserDefaults.SetString(token, "token");
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        static string DictionaryToJson(NSDictionary dictionary)
        {
            NSError error;
            var json = NSJsonSerialization.Serialize(dictionary, NSJsonWritingOptions.PrettyPrinted, out error);

            return json.ToString(NSStringEncoding.UTF8);
        }
    }
}