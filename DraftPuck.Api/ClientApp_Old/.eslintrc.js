module.exports = {
	root: true,
	env: {
		node: true
	},
	extends: [
		'plugin:vue/essential',
		'eslint:recommended',
		'@vue/typescript/recommended'
	],
	globals: {
		google: "readonly"
    },
	parserOptions: {
		ecmaVersion: 2020,
		sourceType: 'module'
	},
	rules: {
		'no-console': process.env.NODE_ENV === 'production' ? 'error' : 'off',
		'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
		'@typescript-eslint/interface-name-prefix': 0,
		'@typescript-eslint/no-var-requires': 0,
		"@typescript-eslint/no-this-alias": 0
	}
}