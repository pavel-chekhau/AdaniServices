﻿using System;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using CoreLib.Commands;
using CoreLib.Helpers;
using CoreLib.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel.Channels;
using System.Xml.Serialization;
using CoreLib.Commands.Common;
using CoreLib.Commands.User;
using CoreLib.Entity;
using CoreLib.Senders;

namespace Tests
{
    [TestClass]
    public class LoginServiceTests
    {
        LogSender senderlog=new LogSender(BroadcastHelper.GetBroadcastIp(),4999);
        public string AuthorizeUser()
        {
            string strSessionKey;
            try
            {
                //здесь кусок кода из теста AuthorizationTest, вынес сюда для сокращения объемов других тестов
            var accessBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 255 };
            Int64 accessLevel = BitConverter.ToInt64(accessBytes, 0);
            var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
            sender.GetTcpSettings();
            var newUser = new User()
            {
                Login = "felias",
                Password = "fenris",
                Name = "pavel",
                AccessLevel = accessLevel,
            };
            var addUserCommand = new UserCommand()
            {
                Command = CommandActions.AddUser,
                User = newUser
            };
            string addUserCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(addUserCommand);
            sender.SendTcpCommand(addUserCommandXml);
            byte[] bytes = sender.ReceiveData();
            var authCommand = new UserCommand()
            {
                Command = CommandActions.Authorization,
                Login = "felias",
                Password = "fenris"
            };
            string authCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(authCommand);
            sender.SendTcpCommand(authCommandXml);
            bytes = sender.ReceiveData();
            strSessionKey = Encoding.ASCII.GetString(bytes);
            senderlog.SendString("Login complete",strSessionKey);
            return strSessionKey;
            }
            catch (Exception ex)
            {
                strSessionKey = "Exception";
                senderlog.SendException(ex,"Login failed, no Session Key");
                return strSessionKey;
            }
        }

        [TestMethod]
        public void SerializeDeserializeTest()
        {

            var testInstance = new TestClass()
            {
                Name = "Test",
                Value = 1
            };

            Stream stream = XmlSerializer<TestClass>.Serialize(testInstance);
            TestClass deserializeInstance = XmlSerializer<TestClass>.Deserialize(stream);

            Assert.IsTrue(testInstance.Name == deserializeInstance.Name);
            Assert.IsTrue(testInstance.Value == deserializeInstance.Value);
        }

        [TestMethod]

        public void GetTcpSettingsTest()
        {
            try
            {
                {
                    var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
                    sender.GetTcpSettings();
                    senderlog.SendString("GetTcpSetting complete", AuthorizeUser());
                }
            }
            catch (Exception ex)
            {
                senderlog.SendException(ex, "GetTcpSetting Failed");
            }
        }

        [TestMethod]
        public void AuthorizationTest()
        {
            try
            {
                var accessBytes = new byte[] {0, 0, 0, 0, 0, 0, 0, 255};
                Int64 accessLevel = BitConverter.ToInt64(accessBytes, 0);

                var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
                sender.GetTcpSettings();
                //создание юзера
                var newUser = new User()
                {
                    Login = "felias",
                    Password = "fenris",
                    Name = "pavel",
                    AccessLevel = accessLevel,
                };
                //команда создания юзера
                var addUserCommand = new UserCommand()
                {
                    Command = CommandActions.AddUser,
                    User = newUser
                };
                //сериализация
                string addUserCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(addUserCommand);
                //отрпавка команды
                sender.SendTcpCommand(addUserCommandXml);
                byte[] bytes = sender.ReceiveData(); //получение ответа
                Assert.IsTrue(Encoding.ASCII.GetString(bytes) == "ok");

                //команда авторизации
                var authCommand = new UserCommand()
                {
                    Command = CommandActions.Authorization,
                    Login = "felias",
                    Password = "fenris"
                };

                string authCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(authCommand);
                sender.SendTcpCommand(authCommandXml);
                //отрпавка команды на авторизацию, в ответ от сервера должен прийти сессионный ключ авторизации
                bytes = sender.ReceiveData();
                string strSessionKey = Encoding.ASCII.GetString(bytes);
                senderlog.SendString("Authorization complete", AuthorizeUser());
            }
            catch (Exception ex)
            {
                senderlog.SendException(ex, "Authorization Failed");
            }
        }

        [TestMethod]
        public void GetAuthInfoTest()
        {
            try
            {
            var accessBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 255 };
            Int64 accessLevel = BitConverter.ToInt64(accessBytes, 0);
            var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
            sender.GetTcpSettings();
            var newUser = new User()
            {
                Login = "felias",
                Password = "fenris",
                Name = "pavel",
                AccessLevel = accessLevel,
            };
            var addUserCommand = new UserCommand()
            {
                Command = CommandActions.AddUser,
                User = newUser
            };
            string addUserCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(addUserCommand);
            sender.SendTcpCommand(addUserCommandXml);
            byte[] bytes = sender.ReceiveData();
            var authCommand = new UserCommand()
            {
                Command = CommandActions.Authorization,
                Login = "felias",
                Password = "fenris"
            };
            string authCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(authCommand);
            sender.SendTcpCommand(authCommandXml);
            bytes = sender.ReceiveData();
            string strSessionKey = Encoding.ASCII.GetString(bytes);

            //команда получения инфы о пользователе
            var userInfoCommand = new ServiceCommand()
            {
                Command = CommandActions.GetUser,
                SessionKey = strSessionKey,
            };

            string userInfoCommandXml = XmlSerializer<ServiceCommand>.SerializeToXmlString(userInfoCommand);

            sender.SendTcpCommand(userInfoCommandXml);
            bytes = sender.ReceiveData();
            string userInfoXml = Encoding.ASCII.GetString(bytes); //инфа о пользователе

            User user = XmlSerializer<User>.Deserialize(userInfoXml); //десериализация строки инфы о пользователе в объект
            Assert.IsTrue(user.Login == authCommand.Login && user.Password == authCommand.Password);
            senderlog.SendString("GetAuthorizationInfo complete", AuthorizeUser());
            }
            catch (Exception ex)
            {
                senderlog.SendException(ex, "GetAuthorizationInfo Failed");
            }
        }

        [TestMethod]
        public void EditUserTest()
        {
            try
            { 
            string strSessionKey = AuthorizeUser();
            var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
            sender.GetTcpSettings();

            //команда получения инфы об авторизации
            var userInfoCommand = new ServiceCommand()
            {
                Command = CommandActions.GetUser,
                SessionKey = strSessionKey
            };

            string userInfoCommandXml = XmlSerializer<ServiceCommand>.SerializeToXmlString(userInfoCommand);

            sender.SendTcpCommand(userInfoCommandXml);
            byte[] btarrResponse = sender.ReceiveData();
            string strUserInfo = Encoding.ASCII.GetString(btarrResponse); //get user information

            User user = XmlSerializer<User>.Deserialize(strUserInfo);
            //изменяем инфу о пользоателе
            user.Login = "newlogin";
            user.Password = "newpasword";

            //command for edit user info
            var editCommand = new UserCommand()
            {
                Command = CommandActions.EditUser,
                User = user
            };
            string editCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(editCommand);
            sender.SendTcpCommand(editCommandXml);
            btarrResponse = sender.ReceiveData();
            string strResponse = Encoding.ASCII.GetString(btarrResponse);

            Assert.IsTrue(strResponse == "ok");
            senderlog.SendString("Edit User complete", AuthorizeUser());
            }
            catch (Exception ex)
            {
                senderlog.SendException(ex, "Edit User Failed");
            }
        }

        [TestMethod]
        public void AddDeleteUserTest()
        {
            try
            {
            string strSessionKey = AuthorizeUser();
            var sender = new CommandSender(BroadcastHelper.GetBroadcastIp(), 4444);
            sender.GetTcpSettings();

            //команда получения инфы о пользователе
            var userInfoCommand = new ServiceCommand()
            {
                Command = CommandActions.GetUser,
                SessionKey = strSessionKey,
            };

            string userInfoCommandXml = XmlSerializer<ServiceCommand>.SerializeToXmlString(userInfoCommand);

            sender.SendTcpCommand(userInfoCommandXml);
            byte[] bytes = sender.ReceiveData();
            string strUserInfo = Encoding.ASCII.GetString(bytes); //инфа о пользователе
            Assert.IsTrue(strUserInfo != "error" && strUserInfo != String.Empty);
            User user = XmlSerializer<User>.Deserialize(strUserInfo); //десериализация строки инфы о пользователе в объект

            //удаление пользователя
            var deleteCommand = new UserCommand()
            {
                Command = CommandActions.RemoveUser,
                UserId = user.Id
            };

            string deleteCommandXml = XmlSerializer<UserCommand>.SerializeToXmlString(deleteCommand);
            sender.SendTcpCommand(deleteCommandXml);
            bytes = sender.ReceiveData();
            Assert.IsTrue(Encoding.ASCII.GetString(bytes) == "ok");
            senderlog.SendString("Add/Delete complete", AuthorizeUser());
            }
            catch (Exception ex)
            {
                senderlog.SendException(ex, "Add/Delete Failed");
            }
        }
    }
}
