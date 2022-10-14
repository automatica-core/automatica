/* SystemJS module definition */
declare var module: NodeModule;
interface NodeModule {
  id: string;
}

declare module '*.json' {
  const value: any;
  export default value;
}

declare module 'globalize' {
  const value: any;
  export default value;
}

declare module 'devextreme/localization/messages/*' {
  const value: any;
  export default value;
}

declare module 'devextreme-cldr-data/*' {
  const value: any;
  export default value;
}