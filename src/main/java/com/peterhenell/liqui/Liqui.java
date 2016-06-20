package com.peterhenell.liqui;

public class Liqui {

	public static void main(String[] args) {
			LiquibaseManager manager = new LiquibaseManager();
			manager.getChanges();
	}

}
