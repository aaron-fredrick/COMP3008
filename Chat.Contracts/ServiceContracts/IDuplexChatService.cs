using System.ServiceModel;
using Chat.Contracts.CallbackContracts;

namespace Chat.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IChatCallback))]
    public interface IDuplexChatService : IChatService
    {
        [OperationContract(IsOneWay = true)]
        void RegisterCallback(string userId);

        [OperationContract(IsOneWay = true)]
        void UnregisterCallback(string userId);
    }
}
