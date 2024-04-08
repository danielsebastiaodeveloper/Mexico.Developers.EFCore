using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Mexico.Developers.Core.Abstractions;

namespace Mexico.Developers.EFCore.Extensions;

/// <summary>
/// Provides a set of extension methods for Atos.EFCore
/// </summary>
public static class EFCoreExtensions
{
    /// <summary>
    /// Sets the traversal properties of an entity that implements the IEntityBase interface
    /// </summary>
    /// <typeparam name="TKey">Type of data that will identify the record</typeparam>
    /// <typeparam name="TUserKey">Type of data that the user will identify</typeparam>
    /// <typeparam name="TEntity">The entity type to be configured.</typeparam>
    /// <param name="userRequired">Default true</param>
    /// <param name="maxLenghtUser">Default 256</param>
    /// <param name="tableName">Table from database</param>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public static void ConfigurationBase<TKey, TUserKey, TEntity>(this EntityTypeBuilder<TEntity> builder, string tableName, bool userRequired = true, int maxLenghtUser = 256)
        where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        builder.ToTable(tableName);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.UserCreatorId).HasMaxLength(maxLenghtUser).IsRequired(userRequired);
        builder.Property(x => x.State).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired();
    }
    /// <summary>
    /// Gets all repositories and registers them in the.net core dependency container
    /// </summary>
    /// <typeparam name="TKey">Type of data that will identify the record</typeparam>
    /// <typeparam name="TUserKey">Type of data that the user will identify</typeparam>
    /// <typeparam name="TContext">Represents a session with the database and can be used to query and save instances of your entities</typeparam>
    /// <param name="services">The IServiceCollection to add services to.</param>
    public static void AddRepositories<TKey, TUserKey, TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        var assembly = typeof(TContext).GetTypeInfo().Assembly;

        var @types = assembly.GetTypes().Where(x => !x.IsNested && !x.IsInterface && typeof(IRepositoryBase<TKey, TUserKey>).IsAssignableFrom(x));

        foreach (var type in @types)
        {
            var @interface = type.GetInterface($"I{type.Name}", false);

            services.AddTransient(@interface, type);
        }
    }
    /// <summary>
    /// Obtenga todas las clases que implementan la interfaz IEntityTypeConfiguration{TEntity} y cree una instancia para invocar el método configure
    /// </summary>
    /// <typeparam name="TContext">Represents a session with the database and can be used to query and save instances of your entities</typeparam>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public static void RegisterEntityConfigurations<TContext>(this ModelBuilder builder)
        where TContext : DbContext
    {
        var assembly = typeof(TContext).GetTypeInfo().Assembly;

        var entityConfigurationTypes = assembly.GetTypes().Where(x => IsEntityTypeConfiguration(x));

        var entityMethod = builder.GetMethodEntity();

        foreach (var entityConfigurationType in entityConfigurationTypes)
        {
            var genericTypeArgument = entityConfigurationType.GetInterfaces().Single().GenericTypeArguments.Single();

            var genericEntityMethod = entityMethod.MakeGenericMethod(genericTypeArgument);

            var entityTypeBuilder = genericEntityMethod.Invoke(builder, null);

            var instance = Activator.CreateInstance(entityConfigurationType);

            instance.GetType().GetMethod(nameof(IEntityTypeConfiguration<object>.Configure)).Invoke(instance, new[] { entityTypeBuilder });
        }
    }
    /// <summary>
    /// Method Extension that get the method Entity of the object ModelBuilder
    /// </summary>
    /// <param name="builder">Object of type <see cref="ModelBuilder"/></param>
    /// <returns>Return an object of type <see cref="MethodInfo"/></returns>
    private static MethodInfo GetMethodEntity(this ModelBuilder builder)
    {
        return builder.GetType().GetMethods().Single(x => x.IsGenericMethod && x.Name == nameof(ModelBuilder.Entity) && x.ReturnType.Name == "EntityTypeBuilder`1");
    }
    /// <summary>
    /// Method extension that validates if the type inheritance is IEntityTypeConfiguration
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Return true if the type inheritanced is of IEntityTypeConfiguration</returns>
    private static bool IsEntityTypeConfiguration(Type type)
    {
        return type.GetInterfaces().Any(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));
    }
}