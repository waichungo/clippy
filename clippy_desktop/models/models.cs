using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Clippy
{
	public partial class ClipItem : ObservableObject
	{
		[ObservableProperty]
		[property: JsonProperty("type")]
		private ClipType _exType = ClipType.TEXT;
		[ObservableProperty]
		[property: JsonProperty("data")]
		private string _data = "";
		[ObservableProperty]
		[property: JsonProperty("device")]
		private string _device = "";
		[ObservableProperty]
		[property: JsonProperty("id")]
		private string _id = "";
		[ObservableProperty]
		[property: JsonProperty("synced")]
		private bool _synced = false;
		[ObservableProperty]
		[property: JsonProperty("createdAt")]
		private DateTime _createdAt = DateTime.Now;
		[ObservableProperty]
		[property: JsonProperty("updatedAt")]
		private DateTime _updatedAt = DateTime.Now;
		public JObject ToJSON()
		{
			var result = new JObject();
			JToken? obj = null;
			result["type"] = new JValue(ExType);
			result["data"] = new JValue(Data);
			result["device"] = new JValue(Device);
			result["id"] = new JValue(Id);
			result["synced"] = new JValue(Synced);
			result["createdAt"] = new JValue(CreatedAt);
			result["updatedAt"] = new JValue(UpdatedAt);
			return result;
		}
		public string ToJSONString(bool indent=false)
		{
			var result = JsonConvert.SerializeObject(ToJSON(),indent?Formatting.Indented:Formatting.None);
			return result;
		}
		public static ClipItem FromJSON(JObject token)
		{
			var result = new ClipItem();
			JToken? valToken=new JObject();
			List<JProperty> props;
			JProperty selected=null;
			if (token.TryGetValue("type", out valToken))
			{
				result.ExType = (ClipType)(valToken.Type != JTokenType.Null ? valToken.Value<long>() : 0L);
			}
			if (token.TryGetValue("data", out valToken))
			{
				result.Data = valToken.Type != JTokenType.Null ? valToken.Value<string>() : "";
			}
			if (token.TryGetValue("device", out valToken))
			{
				result.Device = valToken.Type != JTokenType.Null ? valToken.Value<string>() : "";
			}
			if (token.TryGetValue("id", out valToken))
			{
				result.Id = valToken.Type != JTokenType.Null ? valToken.Value<string>() : "";
			}
			if (token.TryGetValue("synced", out valToken))
			{
				result.Synced = valToken.Type != JTokenType.Null ? valToken.Value<bool>() : false;
			}
			if (token.TryGetValue("createdAt", out valToken))
			{
				result.CreatedAt = valToken.Type != JTokenType.Null ? valToken.Value<DateTime>() : DateTime.Now;
			}
			if (token.TryGetValue("updatedAt", out valToken))
			{
				result.UpdatedAt = valToken.Type != JTokenType.Null ? valToken.Value<DateTime>() : DateTime.Now;
			}
			return result;
		}
		public static ClipItem FromJSONString(string json)
		{
			var token=JToken.Parse(json);
			return ClipItem.FromJSON(token as JObject);
		}
	}
}