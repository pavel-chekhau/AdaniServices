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
using CoreLib.Commands.Common;
using CoreLib.Commands.Settings;
using CoreLib.Encryption;
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
            //дешифровка
            string publicKey = strData.Substring(strData.Length - 8);
            string hash = Encrypter.GeneratePasswordHash(publicKey);
            strData = strData.Substring(0, strData.Length - 8);
            string decryptXml = Encrypter.Decrypt(strData, hash);
            //парсинг результирующего xml
            var xml = new XmlDocument();
            xml.LoadXml(decryptXml);
            XmlNodeList nodeList = xml.GetElementsByTagName("Command");
            var xmlNode = nodeList.Item(0);
            //выбор команды для выполнения
            switch(xmlNode.InnerText) {
            case "GetDeviceSettings":
               GetDeviceSettings(decryptXml);
               break;
            default:
               break;
            }
         }
      }

      private User GetUserInfo(string sessionKey) {
         try {
            var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
            sender.GetTcpSettings();

            var authInfoCommand = new ServiceCommand() {
               Command = CommandActions.GetUserInfo,
               SessionKey = sessionKey
            };

            string strAuthInfoCommand = XmlSerializer<ServiceCommand>.SerializeToXmlString(authInfoCommand);

            sender.SendTcpCommand(strAuthInfoCommand);
            byte[] btarrResponse = sender.ReceiveData();
            string strAuthInfoResult = Encoding.ASCII.GetString(btarrResponse);
            //десериализация xml в объект пользователя
            var userInfo = XmlSerializer<User>.Deserialize(strAuthInfoResult);
            return userInfo;
         }
         catch {
            return null;
         }
      }

      private void GetDeviceSettings(string xml) {
         try {
            var command = XmlSerializer<DeviceSettingsCommand>.Deserialize(xml);

            User user = GetUserInfo(command.SessionKey);
            if(user == null) {
               throw new Exception("Cant get user info");
            }
            Device deviceEntity;
            string xmlString;
            using(var provider = new EntityProvider()) {
               Device device = provider.GetDeviceInfo(command.DeviceId);
               if(device == null) {
                  throw new Exception("Cant get device information");
               }
               xmlString = XmlSerializer<Device>.SerializeToXmlString(device);
            }
            SendResponse(xmlString);
         }
         catch(Exception ex) {
            SendResponse(ex.Message);
         }
      }
   }
}