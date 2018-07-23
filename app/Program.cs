using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace app
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("Starting my proxy server!");

			var listener = new TcpListener(IPAddress.Any, 5000);
			listener.Start();
			Console.WriteLine("Waiting...");
			while (true)
			{
				var socket = await listener.AcceptSocketAsync();
				Console.WriteLine("Connected...");

				var thread = new Thread(() => Handle(socket));
				thread.Priority = ThreadPriority.AboveNormal;
				thread.Start();
			}
		}

		private static readonly Regex _regex = new Regex(@"^(?<verb>.+) (?<scheme>.+)://(?<host>[^ :/]+)(?::(?<port>\d+))?(?<path>/.*) (?<protocol>.+)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		private static void Handle(Socket clientSocket)
		{
			try
			{
				using (var stream = new NetworkStream(clientSocket))
				using (var sr = new StreamReader(stream, ASCIIEncoding.ASCII))
				using (var destinationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					var requestLine = sr.ReadLine();
					Console.WriteLine($"*** 1: {requestLine}");

					var remoteHost = _regex.Match(requestLine).Groups["host"].Value;
					requestLine = _regex.Replace(requestLine, "${verb} ${path} ${protocol}");
					// Console.WriteLine($"*** 2: {requestLine}");


					// Send requestLine
					destinationSocket.Connect(remoteHost, 80);
					destinationSocket.Send(ASCIIEncoding.ASCII.GetBytes(requestLine + "\r\n"));


					// Send headers
					string headerLine = sr.ReadLine();
					while (headerLine.Length > 0)
					{
						destinationSocket.Send(ASCIIEncoding.ASCII.GetBytes(headerLine + "\r\n"));
						headerLine = sr.ReadLine();
					}
					destinationSocket.Send(ASCIIEncoding.ASCII.GetBytes("\r\n"));


					// Send body
					byte[] responseBuffer = new byte[1000];
					while (destinationSocket.Receive(responseBuffer) != 0)
					{
						clientSocket.Send(responseBuffer);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
			}
		}
	}
}
