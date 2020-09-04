using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OTHub.APIServer.Ethereum
{
    public static class AbiHelper
    {
        public static string GetContractAbi(ContractTypeEnum ContractTypeEnum)
        {
            string path = "OTHub.APIServer.Abis.";

            switch (ContractTypeEnum)
            {
                case ContractTypeEnum.Approval:
                    path += "approval.json";
                    break;
                case ContractTypeEnum.Holding:
                    path += "holding.json";
                    break;
                case ContractTypeEnum.HoldingStorage:
                    path += "holding-storage.json";
                    break;
                case ContractTypeEnum.Profile:
                    path += "profile.json";
                    break;
                case ContractTypeEnum.ProfileStorage:
                    path += "profile-storage.json";
                    break;
                case ContractTypeEnum.Replacement:
                    path += "replacement.json";
                    break;
                case ContractTypeEnum.Litigation:
                    path += "litigation.json";
                    break;
                case ContractTypeEnum.LitigationStorage:
                    path += "litigation-storage.json";
                    break;
                case ContractTypeEnum.Token:
                    path += "token.json";
                    break;
                case ContractTypeEnum.ERC725:
                    path += "erc725.json";
                    break;
                default:
                    throw new Exception("Not supported: " + ContractTypeEnum);
            }

            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            using (StreamReader reader = new StreamReader(resource))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
