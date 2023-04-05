CREATE PROCEDURE [dbo].[usp_SaveAccount]
	@Username NVARCHAR(50),
	@Password NVARCHAR(255)
 AS
 BEGIN
	DECLARE @AccountNo BIGINT = CONVERT(NUMERIC(12,0), RAND() * 899999999999) + 100000000000;
	DECLARE @DefaultBalance DECIMAL(18, 2) = 0.00;
	DECLARE @CreatedDate DATETIME = GETDATE();
	WHILE EXISTS (SELECT 1 FROM [dbo].[Account] WHERE AccountNo = @AccountNo)
	BEGIN
		SET @AccountNo = CONVERT(NUMERIC(12,0), RAND() * 899999999999) + 100000000000;
	END

	INSERT INTO [dbo].[Account]
	(
	    AccountNo,
	    Username,
	    [Password],
	    Balance,
	    CreatedDate
	)
	VALUES 
	(
	    @AccountNo,
	    @Username,
	    @Password,
	    @DefaultBalance,
	    @CreatedDate
	)

	SELECT
		[AccountNo],
		[Balance],
		[CreatedDate],
		[Version]
	FROM
		[dbo].[Account]
	WHERE
		AccountNo = @AccountNo
END
GO

CREATE PROCEDURE [dbo].[usp_Deposit]
	@AccountNo			  BIGINT,
	@Balance			  DECIMAL(18,2),	
	@RowVersion			  ROWVERSION,
	@Amount				  DECIMAL(18,2)
AS
BEGIN
	BEGIN TRANSACTION Trans

	SET @Balance += @Amount
	
	INSERT INTO [dbo].[Transaction]
	(
		AccountNo,
		TransactionTypeId,
		Amount,
		EndBalance,
		TransactionDate
	)
	VALUES
	(
		@AccountNo,
		1,
		@Amount,
		@Balance,
		GETDATE()
	)
	
	UPDATE [dbo].[Account] SET Balance = @Balance WHERE AccountNo = @AccountNo AND [Version] = @RowVersion

	IF @@ROWCOUNT = 0
	BEGIN
		ROLLBACK
		RAISERROR('Data has been modified', 16, 1)
		RETURN
	END

	COMMIT
END
GO

CREATE PROCEDURE [dbo].[usp_Withdraw]
	@AccountNo			  BIGINT,
	@Balance			  DECIMAL(18,2),
	@RowVersion			  ROWVERSION,
	@Amount				  DECIMAL(18,2)
AS
BEGIN
	BEGIN TRANSACTION

	DECLARE @ErrorCode INT = 0

	SET @Balance += (@Amount * -1)
	
	INSERT INTO [dbo].[Transaction]
	(
		AccountNo,
		TransactionTypeId,
		Amount,
		EndBalance,
		TransactionDate
	)
	VALUES
	(
		@AccountNo,
		2,
		@Amount,
		@Balance,
		GETDATE()
	)
	
	UPDATE [dbo].[Account] SET Balance = @Balance WHERE AccountNo = @AccountNo AND [Version] = @RowVersion

	IF @@ROWCOUNT = 0
	BEGIN
		ROLLBACK
		RAISERROR('Data has been modified', 16, 1)
		RETURN
	END

	COMMIT
END
GO

CREATE PROCEDURE [dbo].[usp_TransferFunds]
	@AccountNo						BIGINT,
	@Balance						DECIMAL(18,2),
	@RowVersion						ROWVERSION,
	@DestinationAccountNo			BIGINT,
	@DestinationAccountBalance		DECIMAL(18,2),
	@DestinationAccountRowVersion	ROWVERSION,
	@Amount							DECIMAL(18,2)
AS
BEGIN
	BEGIN TRANSACTION Trans

	SET @Balance += (@Amount * -1)
	
	INSERT INTO [dbo].[Transaction]
	(
		AccountNo,
		FromToAccount,
		TransactionTypeId,
		Amount,
		EndBalance,
		TransactionDate
	)
	VALUES
	(
		@AccountNo,
		@DestinationAccountNo,
		3,
		@Amount,
		@Balance,
		GETDATE()
	)
	
	SET @DestinationAccountBalance += @Amount

	INSERT INTO [dbo].[Transaction]
	(
		AccountNo,
		FromToAccount,
		TransactionTypeId,
		Amount,
		EndBalance,
		TransactionDate
	)
	VALUES
	(
		@DestinationAccountNo,
		@AccountNo,
		3,
		@Amount,
		@DestinationAccountBalance,
		GETDATE()
	)

	UPDATE [dbo].[Account] SET Balance = @Balance WHERE AccountNo = @AccountNo AND [Version] = @RowVersion

	IF @@ROWCOUNT = 0
	BEGIN
		ROLLBACK
		RAISERROR('Data has been modified', 16, 1)
		RETURN
	END

	UPDATE [dbo].[Account] SET Balance += @Amount WHERE AccountNo = @DestinationAccountNo AND [Version] = @DestinationAccountRowVersion

	IF @@ROWCOUNT = 0
	BEGIN
		ROLLBACK
		RAISERROR('Data has been modified', 16, 1)
		RETURN
	END
	
	COMMIT
END
GO