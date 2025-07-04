﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using kfrpj.Data;

#nullable disable

namespace kfrpj.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250620191705_CreateSettingsTable2")]
    partial class CreateSettingsTable2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("kfrpj.Models.ViewModels.TenantReportViewModel", b =>
                {
                    b.Property<string>("GeneratedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("GeneratedDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("TotalCount")
                        .HasColumnType("int");

                    b.ToTable("tenant_report_view_models");
                });

            modelBuilder.Entity("kfrpj.Models.rooms.rooms_list", b =>
                {
                    b.Property<int>("room_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("room_id"));

                    b.Property<DateTime>("created_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("created_by")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("record_status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("room_description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("room_image")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("room_name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("room_price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("room_status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("room_type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("updated_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("updated_by")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("room_id");

                    b.ToTable("rooms_list");
                });

            modelBuilder.Entity("kfrpj.Models.settings.settings_list", b =>
                {
                    b.Property<int>("setting_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("setting_id"));

                    b.Property<string>("category")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("created_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("created_by")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("data_type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("record_status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("setting_key")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("setting_name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("setting_value")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("unit")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("updated_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("updated_by")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("setting_id");

                    b.ToTable("settings_list");
                });

            modelBuilder.Entity("kfrpj.Models.sys_users", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<DateTime?>("created_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("created_by")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("email")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("fullname")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("last_login")
                        .HasColumnType("datetime2");

                    b.Property<string>("password")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("phone_number")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("record_status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("role")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("updated_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("updated_by")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("username")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("id");

                    b.ToTable("sys_users");
                });

            modelBuilder.Entity("kfrpj.Models.sys_users_password", b =>
                {
                    b.Property<int>("user_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("user_id"));

                    b.Property<string>("passwordhash")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("user_id");

                    b.ToTable("sys_users_password");
                });

            modelBuilder.Entity("kfrpj.Models.tenants.tenants_list", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("created_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("created_by")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("phone_number")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("record_status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("rental_info")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("updated_at")
                        .HasColumnType("datetime2");

                    b.Property<string>("updated_by")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("tenants_list");
                });
#pragma warning restore 612, 618
        }
    }
}
