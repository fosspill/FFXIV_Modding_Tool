-- Meta table for storing framework values,
-- Such as Cache version.
CREATE TABLE "meta" (
	"key" TEXT NOT NULL UNIQUE,
	"value" TEXT,
	PRIMARY KEY("key")
);

CREATE TABLE "equipment" (
	"equipment_id"	INTEGER NOT NULL,
	"slot"		TEXT NOT NULL,
	"imc_variant"		TEXT NOT NULL,
	"is_set"	INTEGER NOT NULL,
	PRIMARY KEY("equipment_id","slot")
);

CREATE TABLE "weapon" (
	"weapon_id"	INTEGER NOT NULL,
	"body_id"	TEXT NOT NULL,
	PRIMARY KEY("weapon_id","body_id")
);

CREATE TABLE "monster" (
	"monster_id"	INTEGER NOT NULL,
	"body_id"	TEXT NOT NULL,
	PRIMARY KEY("monster_id","body_id")
);

CREATE TABLE "demihuman" (
	"monster_id"		INTEGER NOT NULL,
	"body_id"			TEXT NOT NULL,
	"imc_variant"		TEXT NOT NULL,
	PRIMARY KEY("monster_id","body_id")
);

CREATE TABLE "items" (
	"exd_id"	INTEGER NOT NULL,
	"name"	TEXT NOT NULL,
	"primary_id"	INTEGER NOT NULL,
	"secondary_id"	INTEGER NOT NULL,
	"is_weapon"	INTEGER NOT NULL,
	"slot"		TEXT,
	"slot_full"	TEXT NOT NULL,
	"imc_variant"	INTEGER NOT NULL,
	"icon_id"	INTEGER NOT NULL,
	PRIMARY KEY("name", "exd_id")
);

CREATE TABLE "ui" (
	"name" TEXT NOT NULL,
	"category" TEXT NOT NULL,
	"subcategory" TEXT,
	"path" TEXT,
	"icon_id" INTEGER NOT NULL,
	
	PRIMARY KEY("name", "path", "icon_id")
);

CREATE TABLE "housing" (
	"name" TEXT NOT NULL,
	"category" TEXT NOT NULL,
	"subcategory" TEXT,
	"primary_id"	INTEGER NOT NULL,
	"icon_id"	INTEGER NOT NULL,
	
	PRIMARY KEY("category", "name")
);


CREATE TABLE "monsters" (
	"name" TEXT NOT NULL,
	"category" TEXT NOT NULL,
	"primary_id"	INTEGER NOT NULL,
	"secondary_id"	INTEGER NOT NULL,
	"imc_variant"	INTEGER NOT NULL,
	"model_type" TEXT NOT NULL,
	
	PRIMARY KEY("category", "name", "primary_id", "secondary_id", "imc_variant")
);