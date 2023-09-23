﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ZMap.Store;

public interface IRefresher
{
    // Task Refresh(IEnumerable<IConfiguration> configurations);
    Task Refresh(List<JObject> configurations);
}