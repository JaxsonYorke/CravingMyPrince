using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Assets.data.databaseTypes
{
    [Table("players")]
    public class Player : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public long id { get; set; }

        [Column("device_id")]
        public string device_id { get; set; }

        [Column("display_name")]
        public string display_name { get; set; }

        [Column("created_at")]
        public System.DateTime created_at { get; set; }
    }

    [Table("stats")]
    public class Stats : BaseModel
    {
        [PrimaryKey("player_id")]
        [Column("player_id")]
        public long player_id { get; set; }

        [Column("deaths")]
        public int deaths { get; set; }

        [Column("total_playtime_seconds")]
        public long total_playtime_seconds { get; set; }

        [Column("total_achievements")]
        public int total_achievements { get; set; }
    }

    [Table("achievements")]
    public class achievements : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("name")]
        public string name { get; set; }

        [Column("description")]
        public string description { get; set; }
    }

    [Table("analytics_events")]
    public class AnalyticsEvent : BaseModel
    {
        [PrimaryKey("id")]
        public long id { get; set; }

        [Column("device_id")]
        public string device_id { get; set; }

        [Column("event_name")]
        public string event_name { get; set; }

        [Column("payload")]
        public string payload { get; set; }

        [Column("created_at")]
        public System.DateTime created_at { get; set; }
    }

    [Table("cloud_saves")]
    public class CloudSave : BaseModel
    {
        [PrimaryKey("id")]
        public long id { get; set; }

        [Column("device_id")]
        public string device_id { get; set; }

        [Column("save_slot")]
        public string save_slot { get; set; }

        [Column("data_json")]
        public string data_json { get; set; }

        [Column("updated_at")]
        public System.DateTime updated_at { get; set; }
    }

    [Table("achievedments")]
    public class Achievedment : BaseModel
    {
        [PrimaryKey("player_id")]
        public string player_id { get; set; }

        [PrimaryKey("achievement_id")]
        public int achievement_id { get; set; }

        [Column("achieved_at")]
        public System.DateTime achieved_at { get; set; }
    }
}