
# What is Push Notification?
Push Notification is the standard mechanism for a server app to send a message to a mobile app installation.
Such messages are sent indirectly, through a PNS (Push Notification Service) such as

- Apple: APNs (Apple Push Notification Service)
- Google: FCM (Firebase Cloud Messaging)
- Windows: WNS (Windows Notification Service)


# Zebble.PushNotification

[logo]: https://github.com/Geeksltd/Zebble.PushNotification/raw/master/icon.png "Zebble.PushNotification"
![logo]

This is a Zebble plugin that allow you to use push notification services.

[![NuGet](https://img.shields.io/nuget/v/Zebble.PushNotification.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.PushNotification/)
It is available on NuGet: [https://www.nuget.org/packages/Zebble.PushNotification/](https://www.nuget.org/packages/Zebble.PushNotification/)

# How does it work?

The user should be prompted to allow notifications. Usually the app will show a message to explain the reason and get the user's permission.
Once the user has accepted this, the registration process takes place.

1. The Zebble mobile app registers for push notifications with the PN Service.
1. PNS sends a unique `token` back to the app, which is the **unique id of that installation**.
1. The app will then register the `token` with the server app (via its API).
1. The server app will store this token as an `App Installation Record` in the database, with that user's ID.

At this stage, the server side app will be able to send messages to the installation token by calling the appropriate API on the PNS.
To the server app, that token, is effectively an address to reach the user on a specific device.

Each message/payload (including json data) should not be more than 2KB. 

---

# Registration (Google)

- If you don't already have one, create your app account in the `Firebase Console`.
- A Server API key and a Client ID are automatically generated for the app. This information is packaged in a `google-services.json` file that is automatically downloaded when you click **ADD APP**. Save this file in a safe place.

1. Next to the `Project overview` click on the Cog icon.
2. Select `Cloud Messaging`
3. Under `Web Configuration` generate a key pair. You will need it for your appSettings.json

---

# Registration (Apple)

### App ID
For using apple services you need to register your application in Apple developer portal to have a `Bundle ID`. Follow the instructions in the following link:  
https://developer.apple.com/library/content/documentation/IDEs/Conceptual/AppDistributionGuide/MaintainingProfiles/MaintainingProfiles.html

An `App ID` is a two-part string used to identify one or more apps from a single development team:

- The string consists of a `Team ID` and a `bundle ID Search string` joined together with a "." character.
- `Team ID` is supplied by Apple and is unique to a specific development team.
- `Bundle ID search string` is supplied by you to match either the bundle ID of a single app or a set of bundle IDs for a group of your apps.

---

### Certificate

Read https://github.com/Redth/PushSharp/wiki/How-to-Configure-&-Send-Apple-Push-Notifications-using-PushSharp

- Activate push certificates on the Apple Developer Portal.
- The iOS Application Bundle identifier must be the same corresponding to the profile used for code signing the app.
- Right click on your Zebble iOS project in Visual Studio
- Go to `iOS Application` tab
- Set `Identifier` field to the Bundle ID you set in the previous step
- Now you should have a `.p12` file which can be used in your server side application to send push notifications.

---

# Mobile app

### App.UI\PushNotificationListener.cs

> The project template contains this file. Fill in the blanks. It's fairly self explanatory.

---

### Info.plist
```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
</array>
```
---

### Entitlements.plist

Add the following as the content of dict tag:
```xml
<key>aps-environment</key>
<string>development</string>
```
---

### iOS.csproj
Set the correct `CodesignProvision` in the iOS project properties.

- Go to the `Properties` section of your iOS project in Visual Studio. 
- Under iOS Application tab, scroll down to the "Background Modes" section. Enable `Remote notifications options` and `RemoteNotifications`.

> If you face any problems, check out the helper class on content folder, and `PushNotificationApplicationDelegate.txt`.

---
 
### google-services.json (Android)

Add `google-services.json` to the Android project folder with `Build Action` set to `GoogleServicesJson` (if the GoogleServicesJson build action is not shown, save and close the Solution, then reopen it).

---
 
### Android\Properties\AndroidManifest.xml

```xml
<application android:label="Project Title">
  <receiver android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver" android:exported="false" />
  <receiver android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver" android:exported="true" android:permission="com.google.android.c2dm.permission.SEND">
    <intent-filter>
      <action android:name="com.google.android.c2dm.intent.RECEIVE" />
      <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
      <category android:name="${applicationId}" />
    </intent-filter>
  </receiver>
</application>
```

Note: `${applicationId}` will be replaced with your app's package name automatically during build process.

---

# Server side (ASP.NET)

First, you need to add the [Olive.PushNotification](https://www.nuget.org/packages/Olive.PushNotification/) NuGet package : `Install-Package Olive.PushNotification`.
Now you should add this section to `appsettings.json` file:
```json
"PushNotification": {
	"Apple": {
		"CertificateFile": "...",
		"Environment": "Sandbox",
		"CertificatePassword": "..."
	},
	"Google": {
		"SenderId": "...",
		"AuthToken": "..."
	},
	"Windows": {
		"PackageName": "...",
		"PackageSID": "...",
		"ClientSecret": "..."
	}
}
```
> **Note**: Please notice that depending on your development environment you may use **SandBox** or **Production**

Open `Startup.cs` file and in the `ConfigureServices(...)` method add `services.AddPushNotification();`. it should be something like this:
```csharp
public override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);
    services.AddPushNotification();
}
```

Olive exposes `IPushNotificationService` interface under `Olive.PushNotification` namespace, which enables you to implement push notification sending functionality. In order to send a push notification from your project using Olive, you must have an class which implements `IUserDevice` interface as shown below:

```csharp
public class iOSUserDevice : IUserDevice
{
    public string DeviceType => "iOS";

    public string PushNotificationToken => "Token";
}
```

#### Note
`AddPushNotification` method has another overload accepting an implementation of `ISubscriptionIdResolver`, which will notify you if you used an expired token to send push notifications. Below you'll find a sample implementation:

```c#
public class SubscriptionIdResolver : ISubscriptionIdResolver
{
    public async Task ResolveExpiredSubscription(string oldSubscriptionId, string newSubscriptionId)
    {
        // Find all devices matching old expired id
        var devices = (await Database.Of<Device>()
		.Where(d => d.PushNotificationToken == oldSubscriptionId)
		.GetList()).ToArray();

	if (devices.None()) return;

	newSubscriptionId = newSubscriptionId.OrNullIfEmpty();

	// Replace push notification token in your records, with the updated one or null (if nothing provided 
	await Database.Update(devices, x => x.PushNotificationToken = newSubscriptionId);
    }
}
```

It's better you add your class in **Domain** project under **Logic** folder.

### Sending Push Notification 

To send a push notification you should simply inject `IPushNotificationService` in your class constructor or use `Context.Current.GetService<Olive.PushNotification.IPushNotificationService>();` if you want to have property injection and call `.Send(...)`.

Here we have used class constructor injection as shown below:

```csharp
using Olive.PushNotification;

namespace Domain
{
    public partial class ActionRequest
    {
        readonly IPushNotificationService PushNotificationService;

        public ActionRequest(IPushNotificationService pushNotificationService)
        {
            this.PushNotificationService = pushNotificationService;
        }

		[...]
    }
}
```

```csharp
async void SendPushNotificationService()
{
    var items = new List<iOSUserDevice>
            {
                new iOSUserDevice()
            };

    PushNotificationService.Send("Title", "Geeks push notification", items);
}
```
