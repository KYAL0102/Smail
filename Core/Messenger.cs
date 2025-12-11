using Core.Models;

namespace Core;

public class Messenger
{
    private static readonly Dictionary<string, List<Action<Message>>> _subscribers = new Dictionary<string, List<Action<Message>>>();

    public static void Subscribe(string action, Action<Message> callback)
    {
        if (!_subscribers.ContainsKey(action))
        {
            _subscribers[action] = new List<Action<Message>>();
        }
        _subscribers[action].Add(callback);
    }

    public static void Publish(Message message)
    {
        if (_subscribers.ContainsKey(message.Action))
        {
            foreach (var callback in _subscribers[message.Action])
            {
                callback.Invoke(message);
            }
        }
    }
}