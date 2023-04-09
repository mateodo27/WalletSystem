CREATE TABLE [dbo].[Account](
	[AccountNo] [bigint] NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](255) NOT NULL,
	[Balance] [decimal](18, 2) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_Account] PRIMARY KEY CLUSTERED 
(
	[AccountNo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT [UK_Account_Username] UNIQUE NONCLUSTERED 
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Transaction](
	[TransactionId] [int] IDENTITY(1,1) NOT NULL,
	[AccountNo] [bigint] NOT NULL,
	[FromToAccount] [bigint] NULL,
	[TransactionTypeId] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[EndBalance] [decimal](18, 2) NOT NULL,
	[TransactionDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Transaction] PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TransactionType](
	[TranactionTypeId] [int] IDENTITY(1,1) NOT NULL,
	[TransactionType] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_TransactionType] PRIMARY KEY CLUSTERED 
(
	[TranactionTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Transaction]  WITH CHECK ADD  CONSTRAINT [FK_Account_Transaction_AccountNo] FOREIGN KEY([AccountNo])
REFERENCES [dbo].[Account] ([AccountNo])
GO
ALTER TABLE [dbo].[Transaction] CHECK CONSTRAINT [FK_Account_Transaction_AccountNo]
GO
ALTER TABLE [dbo].[Transaction]  WITH CHECK ADD  CONSTRAINT [FK_Account_Transaction_DestinationAccountNo] FOREIGN KEY([FromToAccount])
REFERENCES [dbo].[Account] ([AccountNo])
GO
ALTER TABLE [dbo].[Transaction] CHECK CONSTRAINT [FK_Account_Transaction_DestinationAccountNo]
GO
ALTER TABLE [dbo].[Transaction]  WITH CHECK ADD  CONSTRAINT [FK_Transaction_TransactionType_TransactionTypeId] FOREIGN KEY([TransactionTypeId])
REFERENCES [dbo].[TransactionType] ([TranactionTypeId])
GO
ALTER TABLE [dbo].[Transaction] CHECK CONSTRAINT [FK_Transaction_TransactionType_TransactionTypeId]
GO