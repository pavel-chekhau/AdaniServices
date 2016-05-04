﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CoreLib.Commands;
using CoreLib.Entity;
using CoreLib.Helpers;
using CoreLib.Serialization;

namespace DeviceSettingsServer.Listeners {
   public class SettingsListener : CommandListener {
      public SettingsListener(int listenPort, IPEndPoint localTcpEp) : base(listenPort, localTcpEp) {
      }

      protected override void Parse(byte[] data) {
         string strData = Encoding.ASCII.GetString(data);
         if(strData == "GET SETTINGS") {
            base.SendTcpSettings();
         }
         else {
            var xml = new XmlDocument();
            xml.LoadXml(strData);
            XmlNodeList nodeList = xml.GetElementsByTagName("Command");
            var xmlNode = nodeList.Item(0);
            var responser = new Responser(_LocalTcpEp);
            switch(xmlNode.InnerText) {
            case "GetDeviceSettings": {
               var deserializer = new XmlSerialization<DeviceSettingsCommand>();
               var command = deserializer.Deserialize(new MemoryStream(data));
               CommandExecute(command);
            }
               break;
            default:
               break;
            }
         }
      }

      protected override void CommandExecute(ServiceCommand command) {
         switch(command.Command) {
            case CommandActions.GetDeviceSettings:
               CheckAuthorization(command);
               break;
         }
      }

      private async Task CheckAuthorization(ServiceCommand command) {
         var settingsCommand = (DeviceSettingsCommand)command;

         var sender = new CommandSender("192.168.1.255", 4444);
         sender.GetTcpSettings();

         var authInfoCommand = new ServiceCommand() {
            Command = CommandActions.AuthorizationInfo,
            SessionKey = settingsCommand.SessionKey
         };

         var serializer = new XmlSerialization<ServiceCommand>();
         string strAuthInfoCommand = serializer.SerializeToXmlString(authInfoCommand);
         
         sender.SendCommand(strAuthInfoCommand);
         byte[] btarrResponse = await sender.ReceiveDataAsync();
         string strAuthInfoResult = Encoding.ASCII.GetString(btarrResponse);


         var responser = new Responser(_LocalTcpEp);
         if(strAuthInfoResult != String.Empty) {
            responser.SendResponse(Encoding.ASCII.GetBytes("ok"));
         }
         else {
            responser.SendResponse(Encoding.ASCII.GetBytes("error"));
         }
      }
   }
}