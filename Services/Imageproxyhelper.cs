namespace BookBoard.Services
{
    public static class ImageProxyHelper
    {
        // Routes a user-pasted image URL through our own server so hotlink
        // protection (based on the browser's Referer header) doesn't block it.
        public static string BuildUrl(string sourceUrl)
        {
            return "/image-proxy?url=" + Uri.EscapeDataString(sourceUrl);
        }
    }
}