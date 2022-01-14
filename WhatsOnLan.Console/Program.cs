using System.Net;
using WhatsOnLan.Core;

Console.WriteLine("Hello, World!");

List<Task> tasks = new List<Task>();

foreach (IPAddress ip in IpAddressHelpers.GetAllHostAddresses(IPAddress.Parse("192.168.1.60"), IPAddress.Parse("255.255.255.0")))
    tasks.Add(Task.Run(() => ArpHelpers.Resolve(ip)));

Task.WaitAll(tasks.ToArray());