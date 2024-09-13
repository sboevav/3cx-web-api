# 3CX Web API / 3CX Phone System Web API 

Fork from Montesuma80/3cx-web-API with updates for use with 3CX v20 Update 2.  
This is untested and I cannot guarantee it will work or be secure.  
 Use at your own risk and strictly limit access to the API.


#### Supportet by v20 Update 2
=======
@All please send me an mail to **hoening`at`googlemail.com

------------


------------


#### Requirements
- Dot Net 8.0
- 3CXPhoneSystem.ini (Debian User must fix the Path in the ini for the 3cxpscomcpp2.dll)

------------


#### Installation

##### Windows

- Download from Microsoft Dot Net 8.0
- Install Dot Net Core
- run in cmd dotnet build WebAPICore.csproj

##### Linux (Debian 12)

```bash
wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update; \ sudo apt-get install -y apt-transport-https && \ sudo apt-get update && \ sudo apt-get install -y dotnet-sdk-8.0
```

**Before compiling, you need to edit the **  *WebAPICore.csproj*
- remove *`<Private>false</Private>`* in <ItemGroup> for 3cxpscomcpp2
- edit path: `<HintPath>..\..\..\Program Files\3CX Phone System\Bin\3cxpscomcpp2.dll</HintPath>` to `<HintPath>/usr/lib/3cxpbx/3cxpscomcpp2.dll</HintPath>` 


```bash
dotnet build WebAPICore.csproj
```

#### Start the API
Now you can start the API.
it is in this path: bin\Debug\net8.0
You need the 3CXPhoneSystem.ini in your API folder

**For Windows User, the API need Admin rights, so start cmd as Administrator.**

dotnet WebAPICore.dll 
or 
dotnet WebAPICore.dll Port

Sample: dotnet WebAPICore.dll 8888 username password

Sample debug mode: dotnet WebAPICore.dll 8888 username password debug

---
#### Run as systemd service (Linux)
If you want to run it as an systemd service you can use this example service file to create your own systemd unit. 
The api listens on port 8888. The access to this port should be strictyl firewalled and only allowed from the 3CX server itself. 
For accessing the API from outside the server, you should use a reverse proxy with authentication.

```
[Unit]
Description=3CX API
Requires=3CXPhoneSystem01.service

[Service]
Type=simple
ExecStartPre=+/usr/sbin/nft add rule inet filter input tcp dport 8843 accept
ExecStart=/usr/bin/dotnet bin/Debug/net8.0/WebAPICore.dll 8888
User=phonesystem
WorkingDirectory=/opt/3cx-web-api/

[Install]
WantedBy=multi-user.target
```
---
#### Features
URL: http://ip:port/action/arg1/arg2/.....

**Action:**
- makecall
- ready
- notready
- logout
- login
- dnregs
- ondn
- getcallerid
- drop
- answer
- record
- transfer
- park
- unpark
- atttrans
- setstatus
- showstatus
- stop

