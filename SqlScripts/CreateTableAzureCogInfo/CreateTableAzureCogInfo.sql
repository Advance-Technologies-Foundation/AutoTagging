IF OBJECT_ID('SysAzureCogInfo') IS NULL
BEGIN
CREATE TABLE [dbo].[SysAzureCogInfo](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedOn] [datetime2](7) NULL,
	[CreatedById] [uniqueidentifier] NULL,
	[ApiKey] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Id_AzureCogInfo] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[SysAzureCogInfo] ADD  CONSTRAINT [DF_Id_AzureCogInfo]  DEFAULT (newid()) FOR [Id]


ALTER TABLE [dbo].[SysAzureCogInfo] ADD  CONSTRAINT [DF_CreatedOn_AzureCogInfo]  DEFAULT (getutcdate()) FOR [CreatedOn]

END;