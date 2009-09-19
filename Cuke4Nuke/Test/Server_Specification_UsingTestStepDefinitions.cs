using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;
using LitJson;

namespace Test
{
    [TestFixture]
    public class Server_Specification_UsingTestStepDefinitions
    {
        Process serverProcess;
        int port = 3902;
        TcpClient client;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            // launch the Cuke4Nuke server in a separate process
            string serverExePath = @"..\..\..\Server\bin\Debug\Cuke4Nuke.Server.exe";
            string stepDefinitionAssemblyPath = @"..\..\..\TestStepDefinitions\bin\Debug\Cuke4Nuke.TestStepDefinitions.dll";
            string commandLineArgs = "-p " + port + " -a \"" + stepDefinitionAssemblyPath + "\"";
            serverProcess = Process.Start(serverExePath, commandLineArgs);

            // connect to the Cuke4Nuke server over TCP
            client = new TcpClient("localhost", port);
        }

        [TestFixtureTearDown]
        public void TestFixtureTeardown()
        {
            // kill the Cuke4Nuke server process, swallowing the exception to avoid the 
            // uncaught exception dialog
            try
            {
                serverProcess.Kill();
            }
            catch (Exception)
            {
            }
        }

        private string SendCommand(string command)
        {
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            StreamReader reader = new StreamReader(stream);
            writer.WriteLine(command);
            writer.Flush();
            string response = reader.ReadLine();
            return response;
        }

        [Test]
        [Timeout(5000)]
        public void ShouldRespondToListStepDefinitionsWithJsonArray()
        {
            string response = SendCommand("list_step_definitions");
            JsonData data = JsonMapper.ToObject(response);
            Assert.That(data.IsArray);
        }

        [Test]
        [Timeout(5000)]
        public void ShouldInvokePassingStepWithOkResponse()
        {
            // get the id of the simple passing step definition
            string stepId = GetStepId("^it should pass.$");

            // invoke that step definition and confirm response is OK
            string invokeCommand = @"invoke:{ ""id"" : """ + stepId + @""" }";
            string stepInvokeResponse = SendCommand(invokeCommand);

            Assert.That(stepInvokeResponse, Is.EqualTo("OK"));
        }

        [Test]
        [Timeout(5000)]
        public void ShouldInvokeNoAssertionStepWithOkResponse()
        {
            // get the id of the simple non-asserting step definition
            string stepId = GetStepId("^nothing$");

            // invoke that step definition and confirm response is OK
            string invokeCommand = @"invoke:{ ""id"" : """ + stepId + @""" }";
            string stepInvokeResponse = SendCommand(invokeCommand);

            Assert.That(stepInvokeResponse, Is.EqualTo("OK"));
        }

        [Test]
        [Timeout(5000)]
        public void ShouldInvokeFailingStepWithFailResponse()
        {
            // get the id of the simple failing step definition
            string stepId = GetStepId("^it should fail.$");

            // invoke that step definition and confirm response is OK
            string invokeCommand = @"invoke:{ ""id"" : """ + stepId + @""" }";
            string stepInvokeResponse = SendCommand(invokeCommand);

            Assert.That(stepInvokeResponse, Is.StringStarting("FAIL:"));
        }

        [Test]
        [Timeout(5000)]
        public void ShouldInvokeStepWithStringParameter()
        {
            // get the id of the simple non-asserting step definition
            string stepId = GetStepId("^a user with name (.*)$");

            // invoke that step definition and confirm response is OK
            string invokeCommand = @"invoke:{ ""id"" : """ + stepId + @""", ""args"" : [ ""foo"" ] }";
            string stepInvokeResponse = SendCommand(invokeCommand);

            Assert.That(stepInvokeResponse, Is.EqualTo("OK"));
        }

        [Test]
        [Timeout(5000)]
        public void ShouldInvokeStepWithIntParameter()
        {
            // get the id of the simple non-asserting step definition
            string stepId = GetStepId(@"^(\d+) cukes$");

            // invoke that step definition and confirm response is OK
            string invokeCommand = @"invoke:{ ""id"" : """ + stepId + @""", ""args"" : [ ""3"" ] }";
            string stepInvokeResponse = SendCommand(invokeCommand);

            Assert.That(stepInvokeResponse, Is.EqualTo("OK"));
        }

        [Test]
        [Timeout(5000)]
        public void ShouldInvokeStepWithMultipleParameters()
        {
            // get the id of the simple non-asserting step definition
            string stepId = GetStepId(@"^(\d+) ounces of (.*) cheese$");

            // invoke that step definition and confirm response is OK
            string invokeCommand = @"invoke:{ ""id"" : """ + stepId + @""", ""args"" : [ ""4"", ""Cheddar"" ] }";
            string stepInvokeResponse = SendCommand(invokeCommand);

            Assert.That(stepInvokeResponse, Is.EqualTo("OK"));
        }

        private string GetStepId(string regexp)
        {
            string stepListResponse = SendCommand("list_step_definitions");
            JsonData stepListJson = JsonMapper.ToObject(stepListResponse);
            string stepId = "";
            for (int i = 0; i < stepListJson.Count; i++)
            {
                if (stepListJson[i]["regexp"].ToString() == regexp)
                {
                    stepId = stepListJson[i]["id"].ToString();
                    break;
                }
            }
            return stepId;
        }
    }
}
