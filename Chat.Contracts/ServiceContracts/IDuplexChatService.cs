using System.ServiceModel;
using Chat.Contracts.CallbackContracts;

namespace Chat.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IChatCallback))]
    public interface IDuplexChatService
    {
        [OperationContract(IsOneWay = true)]
        void RegisterCallback(string userId);

        [OperationContract(IsOneWay = true)]
        void UnregisterCallback(string userId);
    }
}
