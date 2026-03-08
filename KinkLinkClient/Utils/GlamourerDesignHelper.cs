using System;
using System.Globalization;
using System.Text;
using KinkLinkCommon.Dependencies.Glamourer;
using Newtonsoft.Json.Linq;

namespace KinkLinkClient.Utils;

public static class GlamourerDesignHelper
{
    public static GlamourerEquipmentSlot ToEquipmentSlot(string key)
    {
        try
        {
            var parsed = uint.Parse(key, NumberStyles.HexNumber);
            var index = (byte)(parsed >> 16) & 0xFF;
            return (GlamourerEquipmentSlot)(1 << index);
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[GlamourerDesignHelper.FromJObject] Unexpected error, {e}");
            return (GlamourerEquipmentSlot)0;
        }
    }

    public static JObject ToJObject(GlamourerDesign design)
    {
        try
        {
            // Convert to a JToken
            var json = JToken.FromObject(design);

            // Creates a link object with two empty arrays
            json["Links"] = new JObject { ["Before"] = new JArray(), ["After"] = new JArray() };

            // Return the object as a JObject
            return json as JObject ?? new JObject();
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[GlamourerDesignHelper.FromJObject] Unexpected error, {e}");
            return new JObject();
        }
    }

    public static GlamourerDesign? FromJObject(JObject? design)
    {
        try
        {
            // Reject null objects
            if (design is null)
                return null;

            // Copy
            var copy = design.DeepClone();

            // Remove Links
            copy["Links"]?.Parent?.Remove();

            // Create a new domain object
            return copy.ToObject<GlamourerDesign>();
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[GlamourerDesignHelper.FromJObject] Unexpected error, {e}");
            return null;
        }
    }

    public static string ToBase64(GlamourerDesign design)
    {
        var jobject = GlamourerDesignHelper.ToJObject(design);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(jobject.ToString()));
    }

    public static GlamourerDesign? FromBase64(string? base64)
    {
        if (string.IsNullOrEmpty(base64))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var jobject = JObject.Parse(json);
            return GlamourerDesignHelper.FromJObject(jobject);
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[GlamourerDesignHelper.FromBase64] Unexpected error, {e}");
            return null;
        }
    }

    public static string ItemToBase64(KinkLinkClient.Services.WardrobeItem item)
    {
        var json = JToken.FromObject(item);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json.ToString()));
    }

    public static KinkLinkClient.Services.WardrobeItem? FromItemBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var jobject = JObject.Parse(json);
            return jobject.ToObject<KinkLinkClient.Services.WardrobeItem>();
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[GlamourerDesignHelper.FromItemBase64] Unexpected error, {e}");
            return null;
        }
    }
}
