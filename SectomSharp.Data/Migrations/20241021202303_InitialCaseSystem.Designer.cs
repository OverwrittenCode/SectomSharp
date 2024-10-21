﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SectomSharp.Data;
using SectomSharp.Data.Enums;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241021202303_InitialCaseSystem")]
    partial class InitialCaseSystem
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "audit_log_type", new[] { "server", "member", "message", "emoji", "sticker", "channel", "thread", "role" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "bot_log_type", new[] { "warn", "ban", "softban", "timeout", "configuration" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "operation_type", new[] { "create", "update", "delete" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "snowflake_type", new[] { "none", "user", "role", "channel", "bot_log_channel", "audit_log_channel" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("SectomSharp.Data.Models.Case", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("timestamptz");

                    b.Property<decimal?>("LogMessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<BotLogType>("LogType")
                        .HasColumnType("bot_log_type");

                    b.Property<OperationType>("OperationType")
                        .HasColumnType("operation_type");

                    b.Property<decimal>("PerpetratorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<decimal?>("TargetId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id", "GuildId");

                    b.HasIndex("ChannelId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("PerpetratorId");

                    b.HasIndex("TargetId");

                    b.HasIndex("GuildId", "TargetId", "LogType", "OperationType");

                    b.ToTable("Cases");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Guild", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Snowflake", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("now()");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<SnowflakeType>("Type")
                        .HasColumnType("snowflake_type");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.ToTable("Snowflake");

                    b.HasDiscriminator<SnowflakeType>("Type").HasValue(SnowflakeType.None);

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("SectomSharp.Data.Models.AuditLogChannel", b =>
                {
                    b.HasBaseType("SectomSharp.Data.Models.Snowflake");

                    b.Property<AuditLogType>("AuditLogType")
                        .HasColumnType("audit_log_type");

                    b.Property<OperationType>("OperationType")
                        .HasColumnType("operation_type");

                    b.Property<string>("WebhookUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasIndex("GuildId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("Snowflake", t =>
                        {
                            t.Property("OperationType")
                                .HasColumnName("AuditLogChannel_OperationType");
                        });

                    b.HasDiscriminator().HasValue(SnowflakeType.AuditLogChannel);
                });

            modelBuilder.Entity("SectomSharp.Data.Models.BotLogChannel", b =>
                {
                    b.HasBaseType("SectomSharp.Data.Models.Snowflake");

                    b.Property<BotLogType>("BotLogType")
                        .HasColumnType("bot_log_type");

                    b.Property<OperationType?>("OperationType")
                        .HasColumnType("operation_type");

                    b.HasIndex("GuildId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasDiscriminator().HasValue(SnowflakeType.BotLogChannel);
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Channel", b =>
                {
                    b.HasBaseType("SectomSharp.Data.Models.Snowflake");

                    b.HasIndex("GuildId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasDiscriminator().HasValue(SnowflakeType.Channel);
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Role", b =>
                {
                    b.HasBaseType("SectomSharp.Data.Models.Snowflake");

                    b.HasIndex("GuildId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasDiscriminator().HasValue(SnowflakeType.Role);
                });

            modelBuilder.Entity("SectomSharp.Data.Models.User", b =>
                {
                    b.HasBaseType("SectomSharp.Data.Models.Snowflake");

                    b.HasIndex("GuildId");

                    b.HasDiscriminator().HasValue(SnowflakeType.User);
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Case", b =>
                {
                    b.HasOne("SectomSharp.Data.Models.Channel", "Channel")
                        .WithMany("Cases")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SectomSharp.Data.Models.Guild", "Guild")
                        .WithMany("Cases")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SectomSharp.Data.Models.User", "Perpetrator")
                        .WithMany("PerpetratorCases")
                        .HasForeignKey("PerpetratorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SectomSharp.Data.Models.User", "Target")
                        .WithMany("TargetCases")
                        .HasForeignKey("TargetId");

                    b.Navigation("Channel");

                    b.Navigation("Guild");

                    b.Navigation("Perpetrator");

                    b.Navigation("Target");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Guild", b =>
                {
                    b.OwnsOne("SectomSharp.Data.Models.Configuration", "Configuration", b1 =>
                        {
                            b1.Property<decimal>("GuildId")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<int>("Id")
                                .HasColumnType("integer");

                            b1.HasKey("GuildId");

                            b1.ToTable("Guilds");

                            b1.ToJson("Configuration");

                            b1.WithOwner()
                                .HasForeignKey("GuildId");

                            b1.OwnsOne("SectomSharp.Data.Models.WarningConfiguration", "Warning", b2 =>
                                {
                                    b2.Property<decimal>("ConfigurationGuildId")
                                        .HasColumnType("numeric(20,0)");

                                    b2.Property<int>("GeometricDurationMultiplier")
                                        .HasColumnType("integer");

                                    b2.Property<bool>("IsDisabled")
                                        .HasColumnType("boolean");

                                    b2.HasKey("ConfigurationGuildId");

                                    b2.ToTable("Guilds");

                                    b2.WithOwner()
                                        .HasForeignKey("ConfigurationGuildId");

                                    b2.OwnsMany("SectomSharp.Data.Models.WarningThreshold", "Thresholds", b3 =>
                                        {
                                            b3.Property<decimal>("WarningConfigurationConfigurationGuildId")
                                                .HasColumnType("numeric(20,0)");

                                            b3.Property<int>("Id")
                                                .ValueGeneratedOnAdd()
                                                .HasColumnType("integer");

                                            b3.Property<BotLogType>("LogType")
                                                .HasColumnType("bot_log_type");

                                            b3.Property<TimeSpan?>("Span")
                                                .HasColumnType("interval");

                                            b3.Property<int>("Value")
                                                .HasColumnType("integer");

                                            b3.HasKey("WarningConfigurationConfigurationGuildId", "Id");

                                            b3.ToTable("Guilds");

                                            b3.WithOwner()
                                                .HasForeignKey("WarningConfigurationConfigurationGuildId");
                                        });

                                    b2.Navigation("Thresholds");
                                });

                            b1.Navigation("Warning");
                        });

                    b.Navigation("Configuration");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.AuditLogChannel", b =>
                {
                    b.HasOne("SectomSharp.Data.Models.Guild", "Guild")
                        .WithMany("AuditLogChannels")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.BotLogChannel", b =>
                {
                    b.HasOne("SectomSharp.Data.Models.Guild", "Guild")
                        .WithMany("BotLogChannels")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Channel", b =>
                {
                    b.HasOne("SectomSharp.Data.Models.Guild", "Guild")
                        .WithMany("Channels")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Role", b =>
                {
                    b.HasOne("SectomSharp.Data.Models.Guild", "Guild")
                        .WithMany("Roles")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.User", b =>
                {
                    b.HasOne("SectomSharp.Data.Models.Guild", "Guild")
                        .WithMany("Users")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Guild", b =>
                {
                    b.Navigation("AuditLogChannels");

                    b.Navigation("BotLogChannels");

                    b.Navigation("Cases");

                    b.Navigation("Channels");

                    b.Navigation("Roles");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.Channel", b =>
                {
                    b.Navigation("Cases");
                });

            modelBuilder.Entity("SectomSharp.Data.Models.User", b =>
                {
                    b.Navigation("PerpetratorCases");

                    b.Navigation("TargetCases");
                });
#pragma warning restore 612, 618
        }
    }
}
