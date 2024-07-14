using NolowaNetwork.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork
{
    public interface INolowaNetwork
    {
        bool Init(NetworkConfigurationModel configuration);
    }
}
