using System;

namespace AMI.Neitsillia.Areas
{
    class Map
    {
        internal enum TileTypes {Grassy, Caves};
        public TileTypes tileType;
        public int[,] tiles;
        public int size;

        [Newtonsoft.Json.JsonConstructor]
        public Map()
        { }
        public Map(TileTypes v, int s)
        {
            this.tileType = v;
            size = s;
            InitialiseTiles();
        }

        private void InitialiseTiles()
        {
            Random rng = new Random();
            tiles = new int[size,size];
            int minTile = ((int)tileType * -5);
            int maxTile = minTile - 5;
            if (minTile >= -1)
                minTile = -2;
            if (maxTile >= minTile)
                maxTile = minTile-1;

                int j = 0;
                for (int i = 0; j < size; i++)
                {
                    tiles[j, i] = rng.Next(maxTile, minTile + 1);
                    if (i + 1 >= size)
                    { i = -1; j++; }
                }
        }
        internal void ChangeMap(TileTypes newType, int newSize = -1)
        {
            Random rng = new Random();
            int[,] t = new int[newSize, newSize];
            int minTile = ((int)tileType * -5);
            int maxTile = minTile - 5;
            if (minTile >= -1)
                minTile = -2;
            if (maxTile >= minTile)
                maxTile = minTile - 1;
            int j = 0;
            for (int i = 0; j < newSize; i++)
            {
                int current = -2;
                if (i < size && j < size && tiles[j, i] >= -1)
                    current = tiles[j, i];
                if (current < -1)
                    t[j, i] = rng.Next(maxTile, minTile + 1);
                if (i + 1 >= newSize)
                { i = -1; j++; }
            }
            tiles = t;
        }
    }
}