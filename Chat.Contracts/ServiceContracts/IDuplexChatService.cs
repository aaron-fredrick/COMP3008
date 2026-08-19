using System.ServiceModel;

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
