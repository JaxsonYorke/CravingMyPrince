namespace Assets.utils.dataStructures
{
    using System.Collections.Generic;
    using MapDirection = Assets.utils.enums.MapDirection;
    
    [System.Serializable]
    public class DirectionalConnections<T>
    {
        private Dictionary<MapDirection, T> connections;

        public DirectionalConnections()
        {
            connections = new Dictionary<MapDirection, T>();
        }

        public void AddConnection(MapDirection direction, T location)
        {
            connections[direction] = location;
        }

        public T GetConnection(MapDirection direction)
        {
            if (connections.TryGetValue(direction, out T location))
            {
                return location;
            }
            return default;
        }
    }
}