﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Uwuify.DiscordBot.Data;

#nullable disable

namespace Uwuify.DiscordBot.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20240814041017_RateLimitProfile_CommandUses")]
    partial class RateLimitProfile_CommandUses
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Uwuify.DiscordBot.Data.AuditRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Creation")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("AuditRecords");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.Guild", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("Active")
                        .HasColumnType("boolean");

                    b.Property<int>("AuditRecordId")
                        .HasColumnType("integer");

                    b.Property<string>("GuildName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("Snowflake")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long>("UserCount")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AuditRecordId");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.RateLimitProfile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Snowflake")
                        .HasColumnType("numeric(20,0)");

                    b.Property<List<DateTime>>("UsesInUtc")
                        .IsRequired()
                        .HasColumnType("timestamp with time zone[]");

                    b.HasKey("Id");

                    b.ToTable("RateLimitProfiles");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.UptimeReport", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AuditRecordId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("End")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EndReason")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Start")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("AuditRecordId");

                    b.ToTable("UptimeReports");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AuditRecordId")
                        .HasColumnType("integer");

                    b.Property<long>("CommandUses")
                        .HasColumnType("bigint");

                    b.Property<int>("GuildId")
                        .HasColumnType("integer");

                    b.Property<decimal>("Snowflake")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AuditRecordId");

                    b.HasIndex("GuildId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.Guild", b =>
                {
                    b.HasOne("Uwuify.DiscordBot.Data.AuditRecord", "AuditRecord")
                        .WithMany()
                        .HasForeignKey("AuditRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AuditRecord");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.UptimeReport", b =>
                {
                    b.HasOne("Uwuify.DiscordBot.Data.AuditRecord", "AuditRecord")
                        .WithMany()
                        .HasForeignKey("AuditRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AuditRecord");
                });

            modelBuilder.Entity("Uwuify.DiscordBot.Data.User", b =>
                {
                    b.HasOne("Uwuify.DiscordBot.Data.AuditRecord", "AuditRecord")
                        .WithMany()
                        .HasForeignKey("AuditRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Uwuify.DiscordBot.Data.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AuditRecord");

                    b.Navigation("Guild");
                });
#pragma warning restore 612, 618
        }
    }
}
