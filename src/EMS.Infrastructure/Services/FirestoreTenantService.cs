using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services;

public class FirestoreTenantService : ITenantService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreTenantService> _logger;
    private const string CollectionName = "tenants";

    public FirestoreTenantService(FirestoreDb firestoreDb, ILogger<FirestoreTenantService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task<Tenant?> GetTenantByIdAsync(string tenantId)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(tenantId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                return MapToTenant(snapshot);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting tenant by id {tenantId}");
            return null;
        }
    }

    public async Task<Tenant?> GetTenantBySubdomainAsync(string subdomain)
    {
        try
        {
            var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("subdomain", subdomain).Limit(1);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                return MapToTenant(snapshot.Documents[0]);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting tenant by subdomain {subdomain}");
            return null;
        }
    }

    public async Task<Tenant?> CreateTenantAsync(Tenant tenant)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(tenant.Id);
            await docRef.SetAsync(tenant.ToFirestoreDocument());
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating tenant {tenant.Id}");
            return null;
        }
    }

    public async Task<bool> UpdateTenantAsync(Tenant tenant)
    {
        try
        {
            tenant.UpdatedAt = DateTime.UtcNow;
            var docRef = _firestoreDb.Collection(CollectionName).Document(tenant.Id);
            await docRef.SetAsync(tenant.ToFirestoreDocument(), SetOptions.MergeAll);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating tenant {tenant.Id}");
            return false;
        }
    }

    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(tenantId);
            await docRef.DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting tenant {tenantId}");
            return false;
        }
    }

    public async Task<List<Tenant>> GetTenantsAsync()
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
            var tenants = new List<Tenant>();
            foreach (var doc in snapshot.Documents)
            {
                tenants.Add(MapToTenant(doc));
            }
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants");
            return new List<Tenant>();
        }
    }

    private Tenant MapToTenant(DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();
        return new Tenant
        {
            Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? snapshot.Id : snapshot.Id,
            Name = dict.TryGetValue("name", out var name) ? name?.ToString() ?? "" : "",
            Subdomain = dict.TryGetValue("subdomain", out var subdomain) ? subdomain?.ToString() ?? "" : "",
            Email = dict.TryGetValue("email", out var email) ? email?.ToString() ?? "" : "",
            PhoneNumber = dict.TryGetValue("phoneNumber", out var phone) ? phone?.ToString() : null,
            Address = dict.TryGetValue("address", out var addr) ? addr?.ToString() : null,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow,
            IsActive = dict.TryGetValue("isActive", out var active) && active is bool activeBool ? activeBool : true,
            PlatformFeePercentage = dict.TryGetValue("platformFeePercentage", out var pfp) && pfp is double pfpDouble ? pfpDouble : 5.0
        };
    }
}
