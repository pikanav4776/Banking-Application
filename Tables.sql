create table Account(
    acc_id int identity(1,1),
	username varchar(25),
	pasword varchar(20),
	routing_number bigint,
	account_number bigint,
	acc_name varchar(80),
	acc_balance decimal(9,2),
	is_active bit,
	email varchar(40),
	home_address varchar(70),
	SSN bigint
);

create table Transactions
(
    id int identity(1,1),
    Name varchar(80),
    account_number bigint,
    date_time datetime,
    acc_name varchar(80),
    description varchar(200)
);

create table Checkbook
(
    id int identity(1,1),
    Payee varchar(80),
    Date datetime,
    Amount decimal(9,2),
    Memo varchar(200),
    Signature varchar(80),
    routingNumber bigint,
    accountNumber bigint
);


create table ServiceRequest(
    service_request_id int identity(1,1),
    account_number bigint,
    requestor varchar(25),
    request_type varchar(20),
    description varchar(200),
    date_sent datetime,
    date_responded datetime null,
    accepted bit null
);
