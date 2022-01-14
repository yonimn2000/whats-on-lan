﻿using System.Net;
using WhatsOnLan.Core;

Console.WriteLine("Hello, World!");

List<Task> tasks = new List<Task>();

foreach (IPAddress ip in IpAddressHelpers.GetAllHostAddresses(IPAddress.Parse("192.168.1.60"), IPAddress.Parse("255.255.255.0")))
    tasks.Add(Task.Run(() =>
    {
        bool resolves = ArpHelpers.Resolves(ip);
        bool pings = PingHelpers.PingIpAddress(ip);
        Console.WriteLine(ip + "\t" + (resolves ? "R" : "-") +(pings ? "P" : "-"));
    }));

Task.WaitAll(tasks.ToArray());