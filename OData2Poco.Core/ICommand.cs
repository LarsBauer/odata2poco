﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OData2Poco.CommandLine
{
    interface IPocoCommand
    {
        Task Execute();
    }
}
