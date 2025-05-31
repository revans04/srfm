module.exports = {
  root: true,
   // https://eslint.vuejs.org/user-guide/#how-to-use-custom-parser
  // Must use parserOptions instead of "parser" to allow vue-eslint-parser to keep working
  parser: 'vue-eslint-parser',
  parserOptions: {
    // https://github.com/typescript-eslint/typescript-eslint/tree/master/packages/parser#configuration
    // https://github.com/TypeStrong/fork-ts-checker-webpack-plugin#eslint
    // Needed to make the parser take into account 'vue' files
    extraFileExtensions: ['.vue'],
    parser: '@typescript-eslint/parser',
    project: './tsconfig.json',
    tsconfigRootDir: __dirname,
    ecmaVersion: 2018, // Allows for the parsing of modern ECMAScript features
    sourceType: 'module' // Allows for the use of imports
  },

  // env: {
  //   browser: true,
  //   node: true
  // },

  // // Rules order is important, please avoid shuffling them
  // extends: [
  //   // Base ESLint recommended rules
  //   // 'eslint:recommended',

  //   // https://github.com/typescript-eslint/typescript-eslint/tree/master/packages/eslint-plugin#usage
  //   // ESLint typescript rules
  //   'plugin:@typescript-eslint/eslint-recommended',
  //   'plugin:@typescript-eslint/recommended',
  //   // consider disabling this class of rules if linting takes too long
  //   'plugin:@typescript-eslint/recommended-requiring-type-checking',

  //   // Uncomment any of the lines below to choose desired strictness,
  //   // but leave only one uncommented!
  //   // See https://eslint.vuejs.org/rules/#available-rules
  //   'plugin:vue/vue3-essential', // Priority A: Essential (Error Prevention)
  //   // 'plugin:vue/strongly-recommended', // Priority B: Strongly Recommended (Improving Readability)
  //   // 'plugin:vue/recommended', // Priority C: Recommended (Minimizing Arbitrary Choices and Cognitive Overhead)

  //   // https://github.com/prettier/eslint-config-prettier#installation
  //   // usage with Prettier, provided by 'eslint-config-prettier'.
  //   'prettier'    
  // ],

  // plugins: [
  //   // required to apply rules which need type information
  //   '@typescript-eslint',

  //   // https://eslint.vuejs.org/user-guide/#why-doesn-t-it-work-on-vue-file
  //   // required to lint *.vue files
  //   'vue',

  //   // https://github.com/typescript-eslint/typescript-eslint/issues/389#issuecomment-509292674
  //   // Prettier has not been included as plugin to avoid performance impact
  //   // add it as an extension for your IDE
  // ],

  rules: {
    'max-len': ['warn', { code: 160 }]
  }
};