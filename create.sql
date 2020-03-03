create table texts(
	   id integer primary key unique,
	   filepath text not null unique,
	   contents text not null,
	   contents_hash text not null
);
create table morphemes(
	   id integer primary key,
	   value text not null unique
);
create table text_morphemes(
	   morpheme_id integer,
	   text_id integer,
	   score double,
	   tf double,
	   idf double,
	   primary key(morpheme_id, text_id)
);
