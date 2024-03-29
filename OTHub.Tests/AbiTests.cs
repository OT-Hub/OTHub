using System;
using OTHub.Settings;
using OTHub.Settings.Abis;
using Xunit;

namespace OTHub.Tests
{
    public class AbiTests
    {
        [Theory]
        [InlineData(ContractTypeEnum.Approval)]
        [InlineData(ContractTypeEnum.ERC725)]
        [InlineData(ContractTypeEnum.Holding)]
        [InlineData(ContractTypeEnum.HoldingStorage)]
        [InlineData(ContractTypeEnum.Litigation)]
        [InlineData(ContractTypeEnum.LitigationStorage)]
        [InlineData(ContractTypeEnum.Profile)]
        [InlineData(ContractTypeEnum.ProfileStorage)]
        [InlineData(ContractTypeEnum.Reading)]
        //[InlineData(ContractTypeEnum.ReadingStorage)] //This doesn't exist?
        [InlineData(ContractTypeEnum.Replacement)]
        [InlineData(ContractTypeEnum.Token)]
        [InlineData(ContractTypeEnum.Hub)]
        public void GetContractAbi(ContractTypeEnum type)
        {
            string abi = AbiHelper.GetContractAbi(type, BlockchainType.Ethereum, BlockchainNetwork.Mainnet);

            Assert.NotNull(abi);
            Assert.NotEmpty(abi);

            abi = AbiHelper.GetContractAbi(type, BlockchainType.Ethereum, BlockchainNetwork.Rinkeby);

            Assert.NotNull(abi);
            Assert.NotEmpty(abi);

            abi = AbiHelper.GetContractAbi(type, BlockchainType.xDai, BlockchainNetwork.Mainnet);

            Assert.NotNull(abi);
            Assert.NotEmpty(abi);
        }
    }
}