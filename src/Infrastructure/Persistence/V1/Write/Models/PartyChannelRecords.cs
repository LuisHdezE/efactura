using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

[Table("v1_party_addresses")]
[Index(nameof(PartyId), nameof(Kind), nameof(Primary), Name = "IX_v1_party_address_party_kind_primary")]
public sealed class V1PartyAddressRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public Guid PartyId { get; set; }
    public int Kind { get; set; }

    [Required]
    [MaxLength(255)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Region { get; set; }

    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    public bool Primary { get; set; }

    [ForeignKey(nameof(PartyId))]
    public V1PartyRecord Party { get; set; } = null!;
}

[Table("v1_party_contacts")]
[Index(nameof(PartyId), nameof(TypeCode), nameof(Primary), Name = "IX_v1_party_contact_party_type_primary")]
public sealed class V1PartyContactRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public Guid PartyId { get; set; }

    [Required]
    [MaxLength(80)]
    public string TypeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    public bool Primary { get; set; }

    [ForeignKey(nameof(PartyId))]
    public V1PartyRecord Party { get; set; } = null!;
}
