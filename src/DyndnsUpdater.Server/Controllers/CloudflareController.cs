using CloudFlare.Client;
using CloudFlare.Client.Api.Result;
using CloudFlare.Client.Api.Zones;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Enumerators;
using Microsoft.AspNetCore.Mvc;

namespace DyndnsUpdater.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CloudflareController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> UpdateZoneRecord(string? token = null, string? zone = null, string? record = null, string? ipv4 = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("token query parameter is missing.");
            if (string.IsNullOrWhiteSpace(zone))
                return BadRequest("zone query parameter is missing.");
            if (string.IsNullOrWhiteSpace(record))
                return BadRequest("record query parameter is missing.");
            if (string.IsNullOrWhiteSpace(ipv4))
                return BadRequest("ipv4 query parameter is missing.");

            using var client = new CloudFlareClient(token);

            var zones = await client.Zones.GetAsync(new ZoneFilter { Name = zone });
            if (zones is null || !zones.Success)
                return StatusCode(StatusCodes.Status424FailedDependency, new { Description = "Failed to get zones from cloudflare", Response = zones });
            if (zones.Result.Count < 1)
                return NotFound($"A zone with the name \"{zone}\" was not found.");

            var recordFullName = $"{record}.{zone}";
            var records = await client.Zones.DnsRecords.GetAsync(zones.Result[0].Id, new DnsRecordFilter
            {
                Name = recordFullName,
            });
            if (records is null || !records.Success)
                return StatusCode(StatusCodes.Status424FailedDependency, new { Description = $"Failed to get records from zone \"{zones.Result[0].Id}\" from cloudflare", Response = records });

            CloudFlareResult<DnsRecord> result;
            if (records.Result.Count < 1)
            {
                result = await client.Zones.DnsRecords.AddAsync(
                    zones.Result[0].Id,
                    new NewDnsRecord
                    {
                        Name = recordFullName,
                        Type = DnsRecordType.A,
                        Proxied = false,
                        Content = ipv4,
                    });
            }
            else
            {
                result = await client.Zones.DnsRecords.UpdateAsync(
                    zones.Result[0].Id,
                    records.Result[0].Id,
                    new ModifiedDnsRecord
                    {
                        Name = recordFullName,
                        Type = DnsRecordType.A,
                        Proxied = false,
                        Content = ipv4,
                    });
            }

            if (result is null || !result.Success)
                return StatusCode(StatusCodes.Status424FailedDependency, new { Description = $"Failed to add or update record \"{recordFullName}\" of zone \"{zones.Result[0].Id}\" on cloudflare", Response = result });
            return Ok();
        }
    }
}
